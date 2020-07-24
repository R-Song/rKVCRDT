using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RAC.Network
{

    // represent a packet of request
    public class MessagePacket
    {
        public readonly string identifier = "-RAC-\n";
        public string from;
        public string to;
        public string length;
        public string content;

        public MessagePacket(byte[] bytes)
        {
            string s = Encoding.Unicode.GetString(bytes);
            s = s.Substring(s.IndexOf(identifier, 0, s.Length, StringComparison.Ordinal));

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
            this.from = string.Format("{0}\n", from);
            this.to = String.Format("{0}\n", to);
            this.content = content;
            this.length = String.Format("{0}\n", content.Length.ToString());
        }

        public byte[] Serialize()
        {
            return Encoding.Unicode.GetBytes(this.identifier + this.from + this.to + this.length + this.content);
        }

        public override string ToString()
        {
            return this.identifier + this.from + this.to + this.length + this.content;
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

        public BufferBlock<byte[]> reqQueue = new BufferBlock<byte[]>();

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

        public void AddToQueue(byte[] bytes)
        {
            reqQueue.Post(bytes);
        }

        public async Task HandleRequestAync()
        {
            byte[] data;

            while (await reqQueue.OutputAvailableAsync())
            {
                data = reqQueue.Receive();
                
                Console.WriteLine("handling ayncshoursly " + data.GetHashCode());
            }

        }

        public void Run()
        {
            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 13;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[20];

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


                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        Console.WriteLine("adding new data");
                        AddToQueue(bytes);
                    }

                    // Shutdown and end connection
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
            }
        }
    }

}