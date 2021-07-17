using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using NetCoreServer;

using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC.Network
{

    public class ClientSession : TcpSession
    {
        private BufferBlock<(String, ClientSession)> reqQueue;
        private BufferBlock<MessagePacket> respQueue;
        private Dictionary<string, TcpSession> activeClients = new Dictionary<string, TcpSession>();


        public ClientSession(TcpServer server, ref BufferBlock<(String, ClientSession)> reqQueue,
        ref BufferBlock<MessagePacket> respQueue) : base(server)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;

        }

        protected override void OnReceived(byte[] buffer, long offset, long i)
        {
            //
            string clientIP = IPAddress.Parse(((IPEndPoint)this.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)this.Socket.RemoteEndPoint).Port.ToString();

            string data = Encoding.Unicode.GetString(buffer, 0, (int)i);
            DEBUG("Receiving the following message:\n" + data);
            reqQueue.Post((data, this));


        }

        protected override void OnDisconnecting()
        {
            // if connection closed
            string clientIP = IPAddress.Parse(((IPEndPoint)this.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)this.Socket.RemoteEndPoint).Port.ToString();
            activeClients.Remove(clientIP);
            DEBUG("Client disconnected");
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session caught an error with code {error}");
        }
    }


    public class TcpHandler : TcpServer
    {
        public BufferBlock<(String, ClientSession)> reqQueue;
        public BufferBlock<MessagePacket> respQueue;

        public TcpHandler(IPAddress address, int port, ref BufferBlock<(String, ClientSession)> reqQueue,
        ref BufferBlock<MessagePacket> respQueue) : base(address, port)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;

        }


        protected override TcpSession CreateSession()
        {
            return new ClientSession(this, ref reqQueue, ref respQueue);
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error}");
        }
    }


    public class Server
    {

        private BufferBlock<(String, ClientSession)> reqQueue;
        private BufferBlock<MessagePacket> respQueue;

        // no need for thread safety cuz one only write and the other only read
        private Dictionary<string, ClientSession> activeClients;
        public Cluster cluster = Global.cluster;

        public TcpHandler tcpHandler;

        public IPAddress address { get; }
        public int port { get; }
        public TcpHandler server;


        // threshold for stop reading if still no starter detected
        private const int readThreshold = 100;

        public Server(Node node)
        {
            this.address = IPAddress.Parse(node.address);
            this.port = node.port;

            this.reqQueue = new BufferBlock<(string, ClientSession)>();
            this.respQueue = new BufferBlock<MessagePacket>();
            
            this.activeClients = new Dictionary<string, ClientSession>();
        }

        public void start()
        {

            this.server = new TcpHandler(this.address, this.port, ref this.reqQueue, ref this.respQueue);

        }

        public void StageResponse(Responses res, string to = "")
        {
            MessagePacket toSent = null;
            for (int i = 0; i < res.destinations.Count; i++)
            {
                Dest dest = res.destinations[i];
                string content = res.contents[i];

                if (dest == Dest.none)
                    continue;
                else if (dest == Dest.client)
                    toSent = new MessagePacket(Global.selfNode.address + ":" + Global.selfNode.port.ToString(),
                                                to, content);
                else if (dest == Dest.broadcast)
                    toSent = new MessagePacket(Global.selfNode.address + ":" + Global.selfNode.port.ToString(),
                                                "", content);
                this.respQueue.Post(toSent);

            }
        }

        public async Task HandleRequestAsync()
        {
            MessagePacket msg;
            string dataStream = "";
            int enderIndex = -1;
            string clientIP = "";


            while (await reqQueue.OutputAvailableAsync())
            {

                (String datastr, ClientSession connection) = reqQueue.Receive();

                dataStream += datastr;
                enderIndex = dataStream.IndexOf("-EOF-");

                // if found -EOF-
                while (enderIndex != -1)
                {
                    // take everything in front of first seen -EOF-
                    string msgstr = dataStream.Substring(0, enderIndex + "-EOF-".Length);
                    int starterIndex = msgstr.LastIndexOf("-RAC-");

                    if (starterIndex != -1)
                    {
                        // take everything between last -RAC- and -EOF-
                        // as a msg
                        msgstr = msgstr.Substring(starterIndex);
                        try
                        {
                            msg = new MessagePacket(msgstr);

                            clientIP = IPAddress.Parse(((IPEndPoint)connection.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)connection.Socket.RemoteEndPoint).Port.ToString();
                            msg.from = clientIP;

                            if (msg.msgSrc == MsgSrc.client && !this.activeClients.ContainsKey(clientIP))
                                activeClients[clientIP] = connection;


                            DEBUG("Msg pushed to be handled:\n" + msgstr);

                            try
                            {
                                Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                                StageResponse(res, msg.from);
                                DEBUG("Resparing response");
                            }
                            catch (OperationCanceledException)
                            {
                                // TODO: handle it 
                                ERROR("Last error caused by message: \n" + msg);
                                continue;
                            }
                            catch (Exception e)
                            {
                                ERROR("Error thrown when handling the request", e, false);
                                ERROR("Last error caused by message: \n" + msg);
                            }

                        }
                        catch (InvalidMessageFormatException e)
                        {
                            WARNING("Parsing of incoming packet fails: " + e.Message + "\n Messages: \n " + msgstr + "\n");

                        }
                    }

                    // remove everything before "-EOF-"
                    dataStream = dataStream.Substring(enderIndex + "-EOF-".Length);
                    // look for next "-EOF-"
                    enderIndex = dataStream.IndexOf("-EOF-");
                }

            }

        }

        public async Task SendResponseAsync()
        {
            MessagePacket toSent;

            while (await this.respQueue.OutputAvailableAsync())
            {
                toSent = this.respQueue.Receive();

                ClientSession dest;

                // broadcast
                if (toSent.to == "")
                {
                    this.cluster.BroadCast(toSent);
                }
                // reply to client, if connection found to be ended, do nothing
                else if (activeClients.TryGetValue(toSent.to.Trim(), out dest))
                {

                    if (dest.IsConnected)
                    {

                        byte[] msg = toSent.Serialize();
                        dest.SendAsync(msg);
                    }
                    // else do nothing
                }
            }
        }

        // void Read(System.Net.Sockets.TcpClient connection)
        // {
        //     NetworkStream stream = connection.GetStream();


        //     if (!connection.Connected)
        //     {
        //         WARNING("New connection error");
        //         return;
        //     }

        //     Byte[] buffer = new Byte[1024];
        //     string data;
        //     int i;

        //     string clientIP = IPAddress.Parse(((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)connection.Client.RemoteEndPoint).Port.ToString();

        //     // try read data
        //     while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
        //     {

        //         data = Encoding.Unicode.GetString(buffer, 0, i);
        //         DEBUG("Reciving the following message:\n" + data);
        //         //reqQueue.Post((data, connection));

        //     }

        //     // if connection closed
        //     activeClients.Remove(clientIP);
        //     connection.Close();
        //     DEBUG("Client disconnected");
        //     return;
        // }

        public void Run()
        {
            try
            {
                // TcpListener server = new TcpListener(port);
                this.server = new TcpHandler(this.address, port, ref reqQueue, ref respQueue);

                // Start listening for client requests.
                server.Start();

                Console.WriteLine("Server Started");

                // Enter the listening loop.
                while (true)
                { 
                    DEBUG("Waiting for a connection... ");
                    Console.ReadLine();
                }
            }
            catch (SocketException e)
            {
                ERROR("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                LOG("Stopped listening");
                this.cluster.DisconnectAll();
                server.Stop();
                this.reqQueue.Complete();
                this.respQueue.Complete();
            }
        }


    }
}
