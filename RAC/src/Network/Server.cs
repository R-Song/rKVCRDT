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
                if (dest == Dest.client)
                {
                    toSent = new MessagePacket(Global.selfNode.address, to, content);
                    this.respQueue.Post(toSent);
                }
                else if (dest == Dest.broadcast)
                {
                    foreach (Node n in Global.cluster)
                    {
                        if (n.isSelf)
                            continue;
                        
                        toSent = new MessagePacket(Global.selfNode.address + ":" + Global.selfNode.port.ToString(),
                                                    n.address.ToString() + ":" + n.port.ToString(), content);

                        this.respQueue.Post(toSent);
                        
                    }
                }
                else if (dest == Dest.none)
                    continue;
            }
        }


        public async Task HandleRequestAsync()
        {
            MessagePacket data;

            while (await reqQueue.OutputAvailableAsync())
            {
                data = reqQueue.Receive();
                Responses res = Parser.RunCommand(data.content, data.msgSrc);
                StageResponse(res, data.from);
                DEBUG("Resparing response");
            }

        }

        public async Task SendResponseAsync()
        {
            MessagePacket toSent;

            while (await this.respQueue.OutputAvailableAsync())
            {
                toSent = this.respQueue.Receive();

                TcpClient dest;

                // reply to client
                if (activeClients.TryGetValue(toSent.to.Trim(), out dest))
                {
                    if (dest.Connected)
                    {
                        DEBUG("Replying:\n " + toSent);
                        byte[] msg = toSent.Serialize();
                        NetworkStream stream = dest.GetStream();

                        stream.Write(msg, 0, msg.Length);
                    }
                    // else do nothing

                }
                else
                { // broadcast
                    DEBUG("Broadcasting:\n " + toSent);
                    String destAddr = toSent.to.Split(":")[0];
                    int destPort = Int32.Parse(toSent.to.Split(":")[1]);

                    try
                    {
                        dest = new TcpClient(destAddr, destPort);
                        dest.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                        dest.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                        Byte[] data = toSent.Serialize();
                        NetworkStream stream = dest.GetStream();
                        // TODO: important!!!!! write sync
                        stream.Write(data, 0, data.Length);

                        stream.Close();
                        dest.Close();
                    }
                    catch (SocketException e)
                    {
                        WARNING("Broadcast fails, Connection to server " + destAddr + ":" +
                        destPort + " cannot be established: " + e.Message);
                    }
                }

                DEBUG("Sent to " + toSent.to);
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
            
            Byte[] buffer = new Byte[1024];
            int i;
            MessagePacket msg = null;
            string data = "";
            int enderIndex = -1;

            // try read data
            while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
            {

                data += Encoding.Unicode.GetString(buffer);
                enderIndex = data.IndexOf("-EOF-");

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
                            DEBUG("Received packet: \n " + msgstr.ToString());
                            reqQueue.Post(msg);
                            string clientIP = msg.from;

                            // if new connection, add to the list
                            if (first && msg.msgSrc == MsgSrc.client)
                            {
                                activeClients[clientIP] = connection;
                                first = false;
                            }
                        }
                        catch (InvalidMessageFormatException e)
                        {
                            WARNING("Parsing of incoming packet fails: " + e.Message);
                        }
                    }

                    data = data.Substring(enderIndex + "-EOF-".Length);
                    enderIndex = data.IndexOf("-EOF-");
                }
            }

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
                server.Stop();
                this.reqQueue.Complete();
                this.respQueue.Complete();
            }
        }
    }

}