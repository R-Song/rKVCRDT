using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using System.Threading;

using Newtonsoft.Json;

using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC.Network
{

    public class Server
    {

        private BufferBlock<MessagePacket> reqQueue = new BufferBlock<MessagePacket>();
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
            MessagePacket data;

            while (await reqQueue.OutputAvailableAsync())
            {
                try
                {
                    data = reqQueue.Receive();
                    Responses res = Parser.RunCommand(data.content, data.msgSrc);
                    StageResponse(res, data.from);
                    DEBUG("Resparing response");
                }
                catch (OperationCanceledException)
                {
                    // promted
                    continue;
                }
                catch (Exception e)
                {
                    ERROR("Error thrown when handling the request" , e, false);
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

                        DEBUG("Sending: " + toSent);

                        stream.Write(msg, 0, msg.Length);
                    }
                    // else do nothing
                }
            }
        }

        void Read(TcpClient connection)
        {
            NetworkStream stream = connection.GetStream();
            bool first = true;

            if (!connection.Connected)
            {
                WARNING("New connection error");
                return;
            }

            Byte[] buffer = new Byte[4096];
            int i;
            MessagePacket msg = null;
            string data = "";
            int enderIndex = -1;
            string clientIP = "";

            // try read data
            while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
            {

                data += Encoding.Unicode.GetString(buffer);
                enderIndex = data.IndexOf("-EOF-");

                DEBUG("Reciving: " + data);

                // if found -EOF-
                while (enderIndex != -1)
                {
                    // take everything in front of first seen -EOF-
                    string msgstr = data.Substring(0, enderIndex + "-EOF-".Length);
                    int starterIndex = msgstr.LastIndexOf("-RAC-");

                    if (starterIndex != -1)
                    {
                        // take everything between last -RAC- and -EOF-
                        // as a msg
                        msgstr = msgstr.Substring(starterIndex);
                        try
                        {
                            msg = new MessagePacket(msgstr);
                            
                            clientIP = IPAddress.Parse (((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString()) + ":" + ((IPEndPoint)connection.Client.RemoteEndPoint).Port.ToString();
                            msg.from = clientIP;

                            // if new connection, add to the client list
                            if (first && msg.msgSrc == MsgSrc.client)
                            {
                                activeClients[clientIP] = connection;
                            }

                            reqQueue.Post(msg);
                            first = false;

                        }
                        catch (InvalidMessageFormatException e)
                        {
                            WARNING("Parsing of incoming packet fails: " + e.Message + "\n Messages: \n " + msgstr + "\n");
                            
                        }
                    }


                    data = data.Substring(enderIndex + "-EOF-".Length);
                    enderIndex = data.IndexOf("-EOF-");
                }
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