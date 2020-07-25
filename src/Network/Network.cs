using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using System.Threading;

namespace RAC.Network
{
    public enum MsgSrc
    {
        server,
        client
    }

    // represent a packet of request
    public class MessagePacket
    {
        public readonly string starter = "-RAC-\n";
        public readonly string ender = "\n-EOF-";
        public string from;
        public string to;
        public MsgSrc msgSrc;
        public string length;
        public string content;
        

        public MessagePacket(string str)
        {
            string s = str;
            // TODO: throw error afterward

            using (StringReader reader = new StringReader(s))
            {
                string line;
                int lineNumeber = 0;
                int cl = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim('\n',' ');

                    switch (lineNumeber)
                    {
                        case 0:
                            // header identifier
                            break;
                        case 1:
                            this.from = line;
                            break;
                        case 2:
                            this.to = line;
                            break;
                        case 3:
                            if (line.Equals("s"))
                                this.msgSrc = MsgSrc.server;
                            else if (line.Equals("c"))
                                this.msgSrc = MsgSrc.client;
                            break;
                        case 4:
                            this.length = line;
                            cl = int.Parse(length);
                            break;
                        case 5:
                            // TODO: simplify this
                            string rest = line + "\n" + reader.ReadToEnd();
                            if (rest.Length == cl)
                            {
                                this.content = rest;
                            }
                            else if (rest.Length < cl)
                            {
                                Console.WriteLine(rest.Length);
                                //TODO: throw error
                                Console.WriteLine("wrong msg length");
                                return;
                            }
                            else
                            {
                                this.content = rest.Substring(0, cl);
                            }

                            break;


                    }
                    lineNumeber++;
                }
            }
        }

        public MessagePacket(string from, string to, string content)
        {
            //this.from = String.Format("{0}:{1}\n", Global.selfNode.address.ToString(), Global.selfNode.port);
            this.from = string.Format("{0}\n", from.Trim('\n',' '));
            this.to = String.Format("{0}\n", to.Trim('\n',' '));
            this.msgSrc = MsgSrc.server; // has to be server;
            this.content = content;
            this.length = String.Format("{0}\n", content.Length.ToString());
        }
        

        public byte[] Serialize()
        {
            string msgSrcstr;
            if (this.msgSrc == MsgSrc.server)
                msgSrcstr = "s\n";
            else
                msgSrcstr = "c\n";

            return Encoding.Unicode.GetBytes(this.starter + this.from + this.to + msgSrcstr + this.length + this.content + this.ender);
        }

        public override string ToString()
        {
            string msgSrcstr;
            if (this.msgSrc == MsgSrc.server)
                msgSrcstr = "s\n";
            else
                msgSrcstr = "c\n";
                
            
            // TODO: make this better
            return "msg content: \n" + this.starter + this.from + this.to + msgSrcstr + this.length + this.content + this.ender;
        }

    }

    public class Node
    {
        public int nodeid;

        public string address;

        public int port;

        public bool isSelf = false;

        public Node(int id, string address, int port)
        {
            this.nodeid = id;
            // TODO: sanity check
            this.address = address;
            this.port = port;

        }


    }


    public class Server
    {

        public BufferBlock<MessagePacket> reqQueue = new BufferBlock<MessagePacket>();
        public BufferBlock<MessagePacket> respQueue = new BufferBlock<MessagePacket>();

        // no need for thread safety cuz one only write and the other only read
        public Dictionary<string, TcpClient> activeClients = new Dictionary<string, TcpClient>();

        private IPAddress address;
        private int port;

        public Server(IPAddress address, int port)
        {
            this.address = address;
            this.port = port;
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
                Response res = Parser.RunCommand(data.content);
                
                for (int i = 0; i < res.destinations.Count; i++)
                {
                    Dest dest = res.destinations[i];
                    string content = res.contents[i];
                    if (dest == Dest.client)
                    {
                        // Do not response server
                        // TODO: simplify this
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

                        toSent = new MessagePacket(Global.selfNode.address, data.from, content);
                        this.respQueue.Post(toSent);
                    }
                    else if (dest == Dest.broadcast)
                    {
                        foreach (Node n in Global.cluster)
                        {
                            if (!n.isSelf)
                            {
                                toSent = new MessagePacket(Global.selfNode.address + ":" + Global.selfNode.port.ToString(), n.address.ToString() + ":" + n.port.ToString(), content);
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
                    Console.WriteLine("Replying " + toSent.to);
                    byte[] msg = toSent.Serialize();
                    NetworkStream stream = dest.GetStream();
                    stream.Write(msg, 0, msg.Length);
                    
                    stream.Close();
                    dest.Close();
                    
                }
                else
                { // broadcast
                    Console.WriteLine("broadcasting " + toSent.to);
                    String destAddr = toSent.to.Split(":")[0];
                    int destPort = Int32.Parse(toSent.to.Split(":")[1]);

                    // TODO: handle this fails
                    dest = new TcpClient(destAddr, destPort);

                    Byte[] data = toSent.Serialize();
                    NetworkStream stream = dest.GetStream();
                    stream.Write(data, 0, data.Length);
                    
                    stream.Close();
                    dest.Close();
                }
                
                Console.WriteLine("Closed! " + toSent.to);
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
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    MessagePacket msg = null;
                    string data = null;
                    int starterIndex = -1;
                    int enderIndex = -1;

                    // TODO: read async?
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {

                        data += Encoding.Unicode.GetString(bytes);  
                        starterIndex = data.IndexOf("-RAC-");
                        enderIndex = data.IndexOf("-EOF-");

                        if ( -1 < starterIndex && starterIndex < enderIndex)  
                        {  
                            data = data.Substring(starterIndex, enderIndex - starterIndex + "-EOF-".Length);
                            // TODO: handle when client starting to send gibberish, goto next connection
                            break;  
                        }  
                    }

                    Console.WriteLine("Received: \n" + data);

                    if (starterIndex <= -1 || enderIndex <= -1 || starterIndex > enderIndex)
                    {
                        goto NextConnection;
                    }
                    
                    msg = new MessagePacket(data);
                    reqQueue.Post(msg);
                    string clientIP = msg.from;

                    if (msg.msgSrc == MsgSrc.client)
                        activeClients[clientIP] = client;      
                        
                    continue;
                    
                NextConnection:
                    stream.Close();
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                Console.WriteLine("Stopped listening");
                server.Stop();
                this.reqQueue.Complete();
            }
        }
    }

}