using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

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
                    switch(lineNumeber)
                    {
                        case 0: 
                            // header identifier
                            break;
                        case 1:
                            this.from = line + "\n";
                            break;
                        case 2:
                            this.to = line + "\n";;
                            break;
                        case 3:
                            this.length = line + "\n";;
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

}