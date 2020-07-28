using System;
using System.IO;
using System.Text;
using RAC.Errors;
using static RAC.Errors.Log;

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
        private const string starter = "-RAC-\n";
        private const string ender = "\n-EOF-";
        public string from { get; }
        public string to { get; }
        public MsgSrc msgSrc { get; }
        public string length { get; }
        public string content {get; }
        

        public MessagePacket(string str)
        {
            string s = str;

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
                            string rest = line + "\n" + reader.ReadToEnd();

                            if (rest.Length == cl)
                                this.content = rest;
                            else if (rest.Length < cl)
                                throw new MessageLengthDoesNotMatchException("Actual content length " + rest.Length + 
                                " is shorter then expected length " + cl);
                            else
                                this.content = rest.Substring(0, cl);

                            break;


                    }
                    lineNumeber++;
                }
            }
        }

        public MessagePacket(string from, string to, string content)
        {
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

            return Encoding.Unicode.GetBytes(starter + this.from + this.to + msgSrcstr + this.length + this.content + ender);
        }

        public override string ToString()
        {
            string msgSrcstr;
            if (this.msgSrc == MsgSrc.server)
                msgSrcstr = "server";
            else
                msgSrcstr = "client";
                
            return "Packet Content:\n" +
            "Source: " + this.from + "\n" +
            "Dest: " + this.to + "\n" +
            "Sender Type: " + msgSrcstr + "\n" +
            "Length: " + this.length + "\n" +
            "Content:\n" + this.content;
            
        }

    }
}