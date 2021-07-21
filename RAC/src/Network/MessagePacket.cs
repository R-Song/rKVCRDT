using System;
using System.Collections.Generic;
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

    // Protocol looks like this
    // \f[FROM IP:PORT]\t[TO IP:PORT]\t[CLASS S\C(server/client)]\t[Content Length]\t[content]\f
    public class MessagePacket
    {
        public string from { get; set; }
        public string to { get; set; }
        public MsgSrc msgSrc { get; }
        public int length { get; }
        public string content { get; }
        
        // Create a message packet from a string with '\f' from beginning and the end pruned
        // This class do not verify that.
        public MessagePacket(string str)
        {
            string s = str;

            string[] fields = str.Split('\t');
            this.from = fields[0];
            this.to = fields[1];
            
            if (fields[2].Equals("s"))
                this.msgSrc = MsgSrc.server;
            else if (fields[2].Equals("c"))
                this.msgSrc = MsgSrc.client;
            else
                throw new InvalidMessageFormatException("Wrong message sender class: " + fields[2]);

            this.length = Int32.Parse(fields[3]);
            this.content = fields[4];

            
            if (fields[4].Length == this.length)
                this.content = fields[4];
            else
                throw new InvalidMessageFormatException("Content length missmatch, actual: " + fields[4].Length + 
                " expected: " + this.length);

            if (fields.Length != 5)
            {
                throw new InvalidMessageFormatException("Number of fields in the given message is incorrect: " + fields.Length);
            }
        }

        public MessagePacket(string from, string to, string content, MsgSrc sender = MsgSrc.server)
        {
            this.from = from;
            this.to = to;
            this.msgSrc = sender; // has to be server;
            this.content = content;
            this.length = content.Length;
        }

        public static List<MessagePacket> ParseReceivedMessage(NetCoreServer.Buffer cache, string clientIP, ref int handled)
        {   
            List<MessagePacket> res = new List<MessagePacket>();

            handled = 0;


            for (int i = 0; i < (int)cache.Size; i++)
            {
                // look for the first "\f"
                if (cache[i] == '\f' && i + 1 < cache.Size && cache[i + 1] != '\f')
                {
                    int loc = i;
                    int len = 1;

                    // look for the last "\f"
                    while(cache[loc + len] != '\f')
                    {
                        len++;
                        if (loc + len > cache.Size)
                            break;
                    }

                    string msgstr = cache.ExtractString(loc, len).Trim('\f');

                    try
                    {
                        MessagePacket msg = new MessagePacket(msgstr);
                        msg.from = clientIP;

                        DEBUG("Msg to be handled:\n" + msgstr);
                        res.Add(msg);

                    }
                    catch (InvalidMessageFormatException e)
                    {
                        WARNING("Parsing of incoming packet fails: " + e.Message + "\n Messages: \n " + msgstr + "\n");
                    }

                    // parts that already parsed
                    i = handled = loc + len;
                }

            }


            return res;
        }
        

        public byte[] Serialize()
        {
            string msgSrcstr;
            if (this.msgSrc == MsgSrc.server)
                msgSrcstr = "s\t";
            else
                msgSrcstr = "c\t";

            return Encoding.UTF8.GetBytes('\f' +
                                              this.from + '\t' +
                                              this.to + '\t' +
                                              msgSrcstr + 
                                              this.length + '\t' +
                                              this.content + '\f');


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
            "Sender Class: " + msgSrcstr + "\n" +
            "Length: " + this.length + "\n" +
            "Content:\n" + this.content;
            
        }

    }
}