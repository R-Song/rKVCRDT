using System;
using System.IO;
using System.Text;

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
}