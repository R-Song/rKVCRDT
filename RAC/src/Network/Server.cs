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

            int handledSize = 0;

            List<MessagePacket> ReceivedMsg = MessagePacket.ParseReceivedMessage(cache, ref handledSize);

            foreach (var msg in ReceivedMsg)
            {
                DEBUG("Msg to be handled:\n" + msg);
                HandleRequest(msg);
            }

            // remove what has been read
            if (handledSize == cache.Size)
                cache.Clear();
            else
                cache.Remove(0, handledSize);


        }

        public void HandleRequest(MessagePacket msg)
        {

            try
            {
                Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                
                DEBUG("Sending responses");
                foreach ((MessagePacket msg, Dest dest) toSent in res.StageResponse())
                {
                    // broadcast
                    if (toSent.dest == Dest.broadcast)
                    {
                        Global.cluster.BroadCast(toSent.msg);
                    }
                    // reply to client
                    else
                    {
                        if (this.IsConnected)
                        {
                            byte[] data = toSent.msg.Serialize();
                            this.SendAsync(data, 0, data.Length);
                        }
                    }
                }

            }
            catch (OperationCanceledException)
            {
                ERROR("Last error caused by message: \n" + msg);
            }
            catch (Exception e)
            {
                ERROR("Error thrown when handling the request", e, false);
                ERROR("Last error caused by message: \n" + msg);
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
