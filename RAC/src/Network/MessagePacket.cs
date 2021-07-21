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
        server = 1,
        client = 2
    }

    // Protocol looks like this
    // \f[4 bytes: MsgSrc][4xN bytes fields for future use][4 bytes: content length][content]
    public class MessagePacket
    {
        // Number of headerfield
        public static int NUM_FIELDS = 2;
        // 1 byte '\f' + each field is 4 bytes * N
        public static int HEADER_SIZE = 1 + NUM_FIELDS * 4;

        public MsgSrc msgSrc { get; }
        public int length { get; }
        public string content { get; }
    

        public MessagePacket(MsgSrc src, int length, string content)
        {
            this.msgSrc = src;
            this.length = length;
            this.content = content;
        }

        // create a msg to send
        public MessagePacket(string content)
        {
            this.msgSrc = MsgSrc.server;
            this.length = content.Length;
            this.content = content;
        }

        public static List<MessagePacket> ParseReceivedMessage(NetCoreServer.Buffer cache, ref int parsedIndex)
        {
            List<MessagePacket> res = new List<MessagePacket>();

            parsedIndex = 0;

            for (int i = 0; i < (int)cache.Size; i++)
            {
                // look for the first "\f"
                if (cache[i] == '\f')
                {   

                    int handledSize = i;
                    
                    // if header cut-off
                    if (i + HEADER_SIZE > cache.Size)
                        break;

                    int srcOffset = i + 1;
                    // length always the last one
                    int contentLengthOffset = i + 1 + (NUM_FIELDS - 1) * 4;

                    try
                    {
                        MsgSrc src = (MsgSrc)BitConverter.ToInt32(cache.Data, srcOffset);
                        int contentlen = BitConverter.ToInt32(cache.Data, contentLengthOffset);

                        // if content cut-off
                        if (i + HEADER_SIZE + contentlen > cache.Size)
                            break;

                        string content = cache.ExtractString(i + HEADER_SIZE, contentlen);

                        MessagePacket msg = new MessagePacket(src, contentlen, content);

                        DEBUG("Msg to be handled:\n" + msg);
                        res.Add(msg);

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
            return res;
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