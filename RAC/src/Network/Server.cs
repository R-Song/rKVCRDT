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
        private BufferBlock<MessagePacket> reqQueue;
        private BufferBlock<MessagePacket> respQueue;
        private Dictionary<string, ClientSession> activeClients;
        private string dataStream = "";
        private string clientIP;


        public ClientSession(TcpServer server, 
        ref BufferBlock<MessagePacket> reqQueue,
        ref BufferBlock<MessagePacket> respQueue,
        ref Dictionary<string, ClientSession> activeClients) : base(server)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;
            this.activeClients = activeClients;
        }

        protected override void OnConnecting()
        {
            this.clientIP = IPAddress.Parse(((IPEndPoint)this.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)this.Socket.RemoteEndPoint).Port.ToString();
            DEBUG("New client from " + this.clientIP + " connected");

        }

        protected override void OnReceived(byte[] buffer, long offset, long i)
        {
            string data = Encoding.Unicode.GetString(buffer, 0, (int)i);
            DEBUG("Receiving the following message:\n" + data);
            this.dataStream += data;


            int enderIndex = dataStream.IndexOf("-EOF-");
            MessagePacket msg;

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
                        msg.from = clientIP;

                        if (msg.msgSrc == MsgSrc.client && (!this.activeClients.ContainsKey(clientIP) || this.activeClients[clientIP].Id != this.Id))
                        {
                            activeClients[clientIP] = this;
                        }

                        DEBUG("Msg pushed to be handled:\n" + msgstr);

                        reqQueue.Post(msg);

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

        protected override void OnDisconnected()
        {
            activeClients.Remove(this.clientIP);
            DEBUG("Client " + this.clientIP + " disconnected");
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session caught an error with code {error}");
        }
    }


    public class TcpHandler : TcpServer
    {
        public BufferBlock<MessagePacket> reqQueue;
        public BufferBlock<MessagePacket> respQueue;
        public Dictionary<string, ClientSession> activeClients;

        public TcpHandler(IPAddress address, int port, 
        ref BufferBlock<MessagePacket> reqQueue,
        ref BufferBlock<MessagePacket> respQueue,
        ref Dictionary<string, ClientSession> activeClients) : base(address, port)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;
            this.activeClients = activeClients;

        }


        protected override TcpSession CreateSession()
        {
            return new ClientSession(this, ref this.reqQueue, ref this.respQueue, ref this.activeClients);
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error}");
        }
    }


    public class Server
    {

        private BufferBlock<MessagePacket> reqQueue;
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

            this.reqQueue = new BufferBlock<MessagePacket>();
            this.respQueue = new BufferBlock<MessagePacket>();

            this.activeClients = new Dictionary<string, ClientSession>();
        }

        public void start()
        {

            this.server = new TcpHandler(this.address, this.port, ref this.reqQueue, ref this.respQueue, ref this.activeClients);

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

            while (await reqQueue.OutputAvailableAsync())
            {
                MessagePacket msg = reqQueue.Receive();;
                try
                {
                    Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                    StageResponse(res, msg.from);
                    DEBUG("Resparing response");
                }
                catch (OperationCanceledException)
                {
                    ERROR("Last error caused by message: \n" + msg);
                    continue;
                }
                catch (Exception e)
                {
                    ERROR("Error thrown when handling the request" , e, false);
                    ERROR("Last error caused by message: \n" + msg);
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

        public void Run()
        {
            try
            {
                // TcpListener server = new TcpListener(port);
                this.server = new TcpHandler(this.address, this.port, ref this.reqQueue, ref this.respQueue, ref this.activeClients);

                // Start listening for client requests.
                server.Start();

                LOG("Server Started");

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
