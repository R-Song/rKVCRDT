using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks.Dataflow;
using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC.Network
{
    public enum MsgSrc
    {
        server = 1,
        client = 2
    }

    // Protocol looks like this
    // \f[4 bytes: MsgSrc][4xN bytes fields for future use][4 bytes: content length][content]
    public class MessagePacket
    {
        // ---packet info-- 
        // Number of headerfield
        public static int NUM_FIELDS = 2;
        // 1 byte '\f' + each field is 4 bytes * N
        public static int HEADER_SIZE = 1 + NUM_FIELDS * 4;

        public MsgSrc msgSrc { get; }
        public int length { get; }
        public string content { get; }

        // --meta data--
        public ClientSession connection { get; set; }
        public Dest to { get; set; }

        // create a msg from received
        public MessagePacket(MsgSrc src, int length, string content, ClientSession from)
        {
            this.msgSrc = src;
            this.length = length;
            this.content = content;
            
            this.connection = from;
        }

        // create a msg to send
        public MessagePacket(string content, Dest to)
        {
            this.msgSrc = MsgSrc.server;
            this.length = content.Length;
            this.content = content;

            this.to = to;
        }

        public static int ParseReceivedMessage(byte[] cache, in ClientSession from)
        {
            int parsedIndex = 0;

            for (int i = 0; i < (int)cache.Length; i++)
            {
                // look for the first "\f"
                if (cache[i] == '\f')
                {   

                    int handledSize = i;
                    
                    // if header cut-off
                    if (i + HEADER_SIZE > cache.Length)
                        break;

                    int srcOffset = i + 1;
                    // length always the last one
                    int contentLengthOffset = i + 1 + (NUM_FIELDS - 1) * 4;

                    try
                    {
                        MsgSrc src = (MsgSrc)BitConverter.ToInt32(cache, srcOffset);
                        int contentlen = BitConverter.ToInt32(cache, contentLengthOffset);

                        // if content cut-off
                        if (i + HEADER_SIZE + contentlen > cache.Length)
                            break;

                        string content = Encoding.UTF8.GetString(cache, i + HEADER_SIZE, contentlen); // cache.ExtractString(i + HEADER_SIZE, contentlen);

                        MessagePacket msg = new MessagePacket(src, contentlen, content, from);

                        Global.server.reqQueue.Post(msg);

                        // next
                        i = HEADER_SIZE + contentlen;
                        parsedIndex = i;

                    }
                    catch (InvalidMessageFormatException e)
                    {
                        WARNING("Parsing of incoming packet fails: " + e.Message);
                        continue; // just look for next '\f'
                    }


                }

            }

            return parsedIndex;
        }


        public byte[] Serialize()
        {
            byte[] srcb = BitConverter.GetBytes((int)this.msgSrc);
            byte[] lenb = BitConverter.GetBytes(this.length);
            byte[] contentb = Encoding.UTF8.GetBytes(this.content);

            List<byte> msgBytes = new List<byte>();
            msgBytes.Add((byte)'\f');
            msgBytes.AddRange(srcb);
            msgBytes.AddRange(lenb);
            msgBytes.AddRange(contentb);

            return msgBytes.ToArray();
        }

        public override string ToString()
        {
            string msgSrcstr;
            if (this.msgSrc == MsgSrc.server)
                msgSrcstr = "server";
            else
                msgSrcstr = "client";

            return "Packet Content:\n" +
            "Sender Class: " + msgSrcstr + "\n" +
            "Length: " + this.length + "\n" +
            "Content:\n" + this.content;

        }

    }
}