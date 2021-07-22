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
        private NetCoreServer.Buffer cache;
        private string clientIP;


        public ClientSession(TcpServer server,
        ref BufferBlock<MessagePacket> reqQueue,
        ref BufferBlock<MessagePacket> respQueue) : base(server)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;
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
            DEBUG("Receiving the following message with length: " + size + " bytes \n" + cache.ToString());

            List<MessagePacket> ReceivedMsg;
            int handledSize = MessagePacket.ParseReceivedMessage(cache, out ReceivedMsg, this);

            foreach (var msg in ReceivedMsg)
            {
                reqQueue.Post(msg);
            }

            if (handledSize == cache.Size)
                cache.Clear();
            else
                cache.Remove(0, handledSize);
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
        public BufferBlock<MessagePacket> reqQueue;
        public BufferBlock<MessagePacket> respQueue;

        public TcpHandler(IPAddress address, int port,
        ref BufferBlock<MessagePacket> reqQueue,
        ref BufferBlock<MessagePacket> respQueue) : base(address, port)
        {
            this.reqQueue = reqQueue;
            this.respQueue = respQueue;
        }


        protected override TcpSession CreateSession()
        {
            return new ClientSession(this, ref this.reqQueue, ref this.respQueue);
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
        }

        public void start()
        {

            this.server = new TcpHandler(this.address, this.port, ref this.reqQueue, ref this.respQueue);

        }




        public async Task HandleRequestAsync()
        {

            while (await reqQueue.OutputAvailableAsync())
            {
                MessagePacket msg = reqQueue.Receive(); ;
                try
                {
                    DEBUG("Resparing response");

                    Responses res = Parser.RunCommand(msg.content, msg.msgSrc);
                    foreach (MessagePacket toSent in res.StageResponse())
                    {
                        // broadcast
                        if (toSent.to == Dest.broadcast)
                        {
                            this.cluster.BroadCast(toSent);
                        }
                        // reply to client, if connection found to be ended, do nothing
                        else if (toSent.to == Dest.client)
                        {
                            if (msg.from.IsConnected)
                            {

                                byte[] data = toSent.Serialize();
                                msg.from.SendAsync(data);
                            }
                            
                        }
                        else
                        {
                            ERROR("Destination DNE for msg: " + msg);
                        }
                    }

                }
                catch (OperationCanceledException)
                {
                    ERROR("Last error caused by message: \n" + msg);
                    continue;
                }
                catch (Exception e)
                {
                    ERROR("Error thrown when handling the request", e, false);
                    ERROR("Last error caused by message: \n" + msg);
                }

            }

        }



        public async Task SendResponseAsync()
        {
            MessagePacket toSent;

            while (await this.respQueue.OutputAvailableAsync())
            {
                // toSent = this.respQueue.Receive();

                // ClientSession dest;

                // // broadcast
                // if (toSent.to == Dest.broadcast)
                // {
                //     this.cluster.BroadCast(toSent);
                // }
                // // reply to client, if connection found to be ended, do nothing
                // else if (activeClients.TryGetValue(toSent.to.Trim(), out dest))
                // {

                //     if (dest.IsConnected)
                //     {

                //         byte[] msg = toSent.Serialize();
                //         dest.Send(msg, 0, msg.Length);
                //     }
                //     // else do nothing
                // }
            }
        }

        public void Run()
        {
            try
            {
                // TcpListener server = new TcpListener(port);
                this.server = new TcpHandler(this.address, this.port, ref this.reqQueue, ref this.respQueue);

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
