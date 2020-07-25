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

    // represent a packet of request
    public class MessagePacket
    {
        public readonly string starter = "-RAC-\n";
        public readonly string ender = "\n-EOF-";
        public string from;
        public string to;
        public string length;
        public string content;

        public MessagePacket(string str)
        {
            string s = str;
            s = s.Substring(s.IndexOf(starter, 0, s.Length, StringComparison.Ordinal));
            // TODO: throw error afterward

            using (StringReader reader = new StringReader(s))
            {
                string line;
                int lineNumeber = 0;
                int cl = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    switch (lineNumeber)
                    {
                        case 0:
                            // header identifier
                            break;
                        case 1:
                            this.from = line + "\n";
                            break;
                        case 2:
                            this.to = line + "\n"; ;
                            break;
                        case 3:
                            this.length = line + "\n"; ;
                            cl = int.Parse(length);
                            break;
                        case 4:
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
            this.content = content;
            this.length = String.Format("{0}\n", content.Length.ToString());
        }
        

        public byte[] Serialize()
        {
            return Encoding.Unicode.GetBytes(this.starter + this.from + this.to + this.length + this.content + this.ender);
        }

        public override string ToString()
        {
            return "msg content: \n" + this.starter + this.from + this.to + this.length + this.content + this.ender;
        }

    }

    public class Node
    {
        public int nodeid;

        public IPAddress address;

        public int port;

        public bool isSelf;


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
                        // TODO: Global.selfNode.address.ToString()
                        toSent = new MessagePacket("ahh", data.from, content);
                    }
                    else if (dest == Dest.broadcast)
                    {
                        Console.WriteLine("Not done here");
                        // TODO: handle this
                    }

                    Console.WriteLine("perparing response");
                    
                    this.respQueue.Post(toSent);
                    
                }
            }   

        }

        public async Task SendResponseAsync()
        {
            MessagePacket data;

            while (await this.respQueue.OutputAvailableAsync())
            {
                data = this.respQueue.Receive();
                // TODO: handle if client DNE (broadcast)
                TcpClient dest = activeClients[data.to.Trim()];
                byte[] msg = data.Serialize();
                dest.GetStream().Write(msg, 0, msg.Length);
                Console.WriteLine("sending res to " + data.to);
                dest.Close();
                Console.WriteLine("Closed! " + data.to);
                
            }   
        }

        public void Run()
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 2020;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

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

                    // TODO: read async?
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                       
                        data += Encoding.Unicode.GetString(bytes);  
                        int starterIndex = data.IndexOf("-RAC-");
                        int enderIndex = data.IndexOf("-EOF-");
                        if ( -1 < starterIndex && starterIndex < enderIndex)  
                        {  
                            break;  
                        }  
                    }

                    msg = new MessagePacket(data);
                    reqQueue.Post(msg);
                    activeClients[msg.from.Trim('\n', ' ')] = client;      
                    
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