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


    public class Node
    {
        public int nodeid;

        public string address;

        public int port;

        public bool isSelf = false;

        [JsonConstructor]
        public Node(int nodeid, string address, int port)
        {
            this.nodeid = nodeid;


            try 
            {
                IPAddress.Parse(address);
            }
            catch(FormatException e)
            {
                ERROR("Node " + nodeid + " provided an incorrect ip address", e);
                throw e;
            }
            
            this.address = address;

            if (port <= 0 || port > 65535)
            {
                ERROR("Node " + nodeid + " provided an incorrect port number of " + port, new ArgumentOutOfRangeException());
            }

            this.port = port;

        }

        public override string ToString() 
        {
            return "Node " + nodeid + ": Address: " + this.address + ":" + this.port + ", is self? " + this.isSelf;
        }

        public static bool DeserializeNodeConfig(string filename, out List<Node> nodes)
        {
            nodes = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(filename));

            // sanity check
            // check if multiple selves
            int selfNodeCount = 0;
            // check if duplicate nodes
            HashSet<string> addrportSet = new HashSet<string>();
            
            foreach (var n in nodes)
            {
                if (n.isSelf)
                    selfNodeCount++;

                if (selfNodeCount > 1)
                {
                    ERROR("Config: Too many self node!");
                    return false;
                }

                string addrport = n.address + n.port.ToString();
                if (addrportSet.Contains(addrport))
                {
                    ERROR("Duplicate nodes!");
                }
                else
                    addrportSet.Add(addrport);


            }

            if (selfNodeCount == 0)
            {
                ERROR("Config: No self node");
                return false;
            }

            StringBuilder listingNodes = new StringBuilder("The following nodes are initalized:\n");
            foreach (var n in nodes)
            {
                listingNodes.AppendLine(n.ToString());
            }

            LOG(listingNodes.ToString());

            return true;

        }


    }


    public class Server
    {

        public BufferBlock<MessagePacket> reqQueue = new BufferBlock<MessagePacket>();
        public BufferBlock<MessagePacket> respQueue = new BufferBlock<MessagePacket>();

        // no need for thread safety cuz one only write and the other only read
        public Dictionary<string, TcpClient> activeClients = new Dictionary<string, TcpClient>();

        private string address;
        private int port;
        
        // threshold for stop reading if still no starter detected
        private const int readThreshold = 100;

        public Server(Node node)
        {
            this.address = node.address;
            this.port = node.port;
        }

        public Server()
        {
            
        }

        public async Task HandleRequestAync()
        {
            MessagePacket data;
            MessagePacket toSent = null;

            while (await reqQueue.OutputAvailableAsync())
            {
                data = reqQueue.Receive();  
                Responses res = Parser.RunCommand(data.content);
                
                for (int i = 0; i < res.destinations.Count; i++)
                {
                    Dest dest = res.destinations[i];
                    string content = res.contents[i];
                    if (dest == Dest.client)
                    {
                        // Do not response server, prob not needed
                        /**
                        bool flag = false;
                        foreach (Node n in Global.cluster)
                        {
                            if (n.address == data.from)
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (flag)
                            continue;
                        **/
                        toSent = new MessagePacket(Global.selfNode.address, data.from, content);
                        this.respQueue.Post(toSent);
                    }
                    else if (dest == Dest.broadcast)
                    {
                        foreach (Node n in Global.cluster)
                        {
                            if (!n.isSelf)
                            {
                                toSent = new MessagePacket(Global.selfNode.address + ":" + Global.selfNode.port.ToString(), 
                                                            n.address.ToString() + ":" + n.port.ToString(), 
                                                            content);
                                this.respQueue.Post(toSent);
                            }
                        }
                    } 
                    else if (dest == Dest.none)
                    {
                        continue;
                    }
                    
                    
                }
                 Console.WriteLine("perparing response");
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
                        stream.Close();
                        dest.Close();
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

                        Byte[] data = toSent.Serialize();
                        NetworkStream stream = dest.GetStream();
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
                
                DEBUG("Closed! " + toSent.to);
            }   
        }

        public void Run()
        {
            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse(Global.selfNode.address);
                Int32 port = Global.selfNode.port;

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
                    DEBUG("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    MessagePacket msg = null;
                    string data = null;
                    int starterIndex = -1;
                    int enderIndex = -1;
                    int byteread = 0;

                    // TODO: read async?
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        byteread += i;

                        data += Encoding.Unicode.GetString(bytes);  
                        starterIndex = data.IndexOf("-RAC-");
                        enderIndex = data.IndexOf("-EOF-");
 
                        if ( -1 < starterIndex && starterIndex < enderIndex)  
                        {  
                            data = data.Substring(starterIndex, enderIndex - starterIndex + "-EOF-".Length);
                            break;  
                        }  

                        if (enderIndex > -1 && starterIndex == -1)
                            goto NextConnection;

                        if (starterIndex == -1 && byteread > readThreshold)
                            goto NextConnection;
                    }

                    
                    if (starterIndex <= -1 || enderIndex <= -1 || starterIndex > enderIndex)
                        goto NextConnection;         
                    
                    try
                    {
                        msg = new MessagePacket(data);
                    }
                    catch (MessageLengthDoesNotMatchException e)
                    {
                        WARNING("Parsing of incoming packet fails: " + e.Message);
                        goto NextConnection;
                    }

                    DEBUG("Received packet: \n " + msg.ToString());
                    reqQueue.Post(msg);
                    string clientIP = msg.from;

                    // TODO: find a way to clean up this
                    if (msg.msgSrc == MsgSrc.client)
                        activeClients[clientIP] = client;      
                        
                    continue;
                    
                NextConnection:
                    WARNING("connection from " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() +
                            ":" + ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString() + 
                            " is disconnected due to incorrect data received: \n" + data);       
                    stream.Close();
                    client.Close();
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