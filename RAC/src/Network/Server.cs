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

        private string clientIP;
        private NetCoreServer.Buffer cache;


        public ClientSession(TcpServer server) : base(server)
        {
            cache = new NetCoreServer.Buffer();
        }

        protected override void OnConnecting()
        {
            this.clientIP = IPAddress.Parse(((IPEndPoint)this.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)this.Socket.RemoteEndPoint).Port.ToString();
            DEBUG("New client from " + this.clientIP + " connected");

        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            cache.Append(buffer, (int)offset, (int)size);
            DEBUG("Receiving the following message:\n" + cache.ToString());

            int last_loc = 0;

            for (int i = 0; i < (int)cache.Size; i++)
            {
                // look for "\f"
                if (cache[i] == '\f' && cache[i + 1] != '\f')
                {
                    int loc = i + 1;
                    int len = 0;
                    last_loc = loc;

                    // look for "-EOF-"
                    while(cache[loc + len] != '\f')
                    {
                        len++;
                        if (loc + len > cache.Size)
                            break;
                    }

                    string req = cache.ExtractString(loc, len).Trim('\f');
                    MessagePacket msg = ParseMsgStr(req);
                    
                    if (!(msg is null))
                    {

                        DEBUG("Handling Request: " + msg);
                        List<MessagePacket> responses = HandleRequest(msg);

                        if (!(responses is null))
                        {
                            this.SendResponses(responses);
                        }
                    }
                    
                    i = loc + len + 1;
                
                }
            }

            // remove what has been read
            cache.Remove(0, last_loc);



        }

        private MessagePacket ParseMsgStr(string msgstr)
        {
            try
            {
                MessagePacket msg = new MessagePacket(msgstr);
                msg.from = clientIP;

                DEBUG("Msg to be handled:\n" + msgstr);

                return msg;

            }
            catch (InvalidMessageFormatException e)
            {
                WARNING("Parsing of incoming packet fails: " + e.Message + "\n Messages: \n " + msgstr + "\n");
                return null;
            }

        }

        public List<MessagePacket> HandleRequest(MessagePacket msg)
        {

            try
            {
                Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                List<MessagePacket> toRes = res.StageResponse(msg.from);
                DEBUG("Resparing response");
                return toRes;
            }
            catch (OperationCanceledException)
            {
                ERROR("Last error caused by message: \n" + msg);
                return null;
            }
            catch (Exception e)
            {
                ERROR("Error thrown when handling the request", e, false);
                ERROR("Last error caused by message: \n" + msg);
                return null;
            }
        }

        public void SendResponses(List<MessagePacket> responses)
        {
            foreach (MessagePacket toSent in responses)
            {
                // broadcast
                if (toSent.to == "")
                {
                    Global.cluster.BroadCast(toSent);
                }
                // reply to client
                else
                {

                    if (this.IsConnected)
                    {
                        byte[] data = toSent.Serialize();
                        this.SendAsync(data, 0, data.Length);
                    }
                }
            }
        }


        protected override void OnDisconnected()
        {
            DEBUG("Client " + this.clientIP + " disconnected");
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session caught an error with code {error}");
        }
    }


    public class TcpHandler : TcpServer
    {



        public TcpHandler(IPAddress address, int port) : base(address, port)
        {
        }


        protected override TcpSession CreateSession()
        {
            return new ClientSession(this);
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error}");
        }





    }


    public class Server
    {

        // no need for thread safety cuz one only write and the other only read

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


        }

        public void Run()
        {
            try
            {
                this.Start();
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
            }
        }
        private void Start()
        {

            this.server = new TcpHandler(this.address, this.port);

            this.server.Start();
            LOG("Server Started");

            while (true)
            {
                DEBUG("Waiting for a connection... ");
                // blocking...
                Console.ReadLine();
            }
        }



    }
}
