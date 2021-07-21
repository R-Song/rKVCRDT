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
        private Dictionary<string, ClientSession> activeClients;
        private string dataStream = "";
        private string clientIP;
        


        public ClientSession(TcpServer server,
        ref Dictionary<string, ClientSession> activeClients) : base(server)
        {
            this.activeClients = activeClients;
        }

        protected override void OnConnecting()
        {
            this.clientIP = IPAddress.Parse(((IPEndPoint)this.Socket.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)this.Socket.RemoteEndPoint).Port.ToString();
            DEBUG("New client from " + this.clientIP + " connected");

        }

        protected override void OnReceived(byte[] buffer, long offset, long i)
        {
            string data = Encoding.Unicode.GetString(buffer, (int)offset, (int)i);
            DEBUG("Receiving the following message:\n" + data);
            this.dataStream += data;

            int enderIndex = dataStream.IndexOf("-EOF-");
            

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
                    MessagePacket msg = ParseMsgStr(msgstr.Substring(starterIndex));

                    if (msg is not null)
                    {
                        List<MessagePacket> responses = HandleRequest(msg);

                        if (responses is not null)
                        {
                            this.SendResponses(responses);
                        }
                    }
                }

                // remove everything before "-EOF-"
                dataStream = dataStream.Substring(enderIndex + "-EOF-".Length);
                // look for next "-EOF-"
                enderIndex = dataStream.IndexOf("-EOF-");
            }
        }

        private MessagePacket ParseMsgStr(string msgstr)
        {
            try
            {
                MessagePacket msg = new MessagePacket(msgstr);
                msg.from = clientIP;

                if (msg.msgSrc == MsgSrc.client && (!this.activeClients.ContainsKey(clientIP) || this.activeClients[clientIP].Id != this.Id))
                {
                    this.activeClients[clientIP] = this;
                }

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


        public Dictionary<string, ClientSession> activeClients;

        public TcpHandler(IPAddress address, int port) : base(address, port)
        {
            this.activeClients = new Dictionary<string, ClientSession>();
        }


        protected override TcpSession CreateSession()
        {
            return new ClientSession(this, ref this.activeClients);
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error}");
        }





    }


    public class Server
    {

        private BufferBlock<MessagePacket> respQueue;

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

            this.respQueue = new BufferBlock<MessagePacket>();

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
