using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;

using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC.Network
{

    public class Server
    {

        private BufferBlock<(String, TcpClient)> reqQueue = new BufferBlock<(String, TcpClient)>();
        private BufferBlock<MessagePacket> respQueue = new BufferBlock<MessagePacket>();

        // no need for thread safety cuz one only write and the other only read
        private Dictionary<string, TcpClient> activeClients = new Dictionary<string, TcpClient>();
        public Cluster cluster = Global.cluster;

        public string address { get; }
        public int port { get; }


        // threshold for stop reading if still no starter detected
        private const int readThreshold = 100;

        public Server(Node node)
        {
            this.address = node.address;
            this.port = node.port;
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

                (String datastr, TcpClient connection) = reqQueue.Receive();

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

                            clientIP = IPAddress.Parse(((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)connection.Client.RemoteEndPoint).Port.ToString();
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

                TcpClient dest;

                // broadcast
                if (toSent.to == "")
                {
                    this.cluster.BroadCast(toSent);
                }
                // reply to client, if connection found to be ended, do nothing
                else if (activeClients.TryGetValue(toSent.to.Trim(), out dest))
                {
                    if (dest.Connected)
                    {
                        byte[] msg = toSent.Serialize();
                        NetworkStream stream = dest.GetStream();

                        DEBUG("Sending the following message:\n" + toSent);

                        stream.Write(msg, 0, msg.Length);
                    }
                    // else do nothing
                }
            }
        }

        void Read(TcpClient connection)
        {
            NetworkStream stream = connection.GetStream();


            if (!connection.Connected)
            {
                WARNING("New connection error");
                return;
            }

            Byte[] buffer = new Byte[1024];
            string data;
            int i;

            string clientIP = IPAddress.Parse(((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)connection.Client.RemoteEndPoint).Port.ToString();

            // try read data
            while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
            {

                data = Encoding.Unicode.GetString(buffer, 0, i);
                DEBUG("Reciving the following message:\n" + data);
                reqQueue.Post((data, connection));

            }

            // if connection closed
            activeClients.Remove(clientIP);
            connection.Close();
            DEBUG("Client disconnected");
            return;
        }

        public void Run()
        {
            TcpListener server = null;
            try
            {
                if (!System.Threading.ThreadPool.SetMinThreads(50, 50) || System.Threading.ThreadPool.SetMaxThreads(100, 100))
                {
                    ERROR("Threadpool setup failed");
                }

                IPAddress localAddr = IPAddress.Parse(this.address);
                Int32 port = this.port;

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[1024];

                // Enter the listening loop.
                while (true)
                {
                    DEBUG("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    DEBUG("New connection!");

                    // start new thread here
                    Task.Run(() => { Read(client); });
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