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
    // TODO: catach other problems (int parser)
    public class MessagePacket
    {
        private const string starter = "-RAC-";
        private const string ender = "-EOF-";
        private const string fromPrefix = "FROM:";
        private const string toPrefix = "TO:";
        private const string classPrefix = "CLS:";
        private const string lengthPrefix = "LEN:";
        private const string contentPrefix = "CNT:";

        public string from { get; }
        public string to { get; set; }
        public MsgSrc msgSrc { get; }
        public int length { get; }
        public string content { get; }
        
        // Create a message packet from a string 
        // starts with "-RAC-" and ends with "-EOF".
        // This class do not verify that.
        public MessagePacket(string str)
        {
            string s = str;
            int numfields = 0;

            using (StringReader reader = new StringReader(s))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    // first line
                    if (line.StartsWith(starter, StringComparison.Ordinal))
                        continue;
                    else if (line.StartsWith(fromPrefix, StringComparison.Ordinal))
                    {
                        this.from = line.Remove(0, fromPrefix.Length).Trim('\n',' ');
                        numfields++;
                    }
                    else if (line.StartsWith(toPrefix, StringComparison.Ordinal))
                    {
                        this.to = line.Remove(0, toPrefix.Length).Trim('\n',' ');
                        numfields++;
                    }
                    else if (line.StartsWith(classPrefix, StringComparison.Ordinal))
                    {
                        line = line.Remove(0, classPrefix.Length).Trim('\n',' ');

                        if (line.Equals("s"))
                            this.msgSrc = MsgSrc.server;
                        else if (line.Equals("c"))
                            this.msgSrc = MsgSrc.client;
                        else
                            throw new InvalidMessageFormatException("Wrong message sender class: " + line);
                        
                        numfields++;
                    }
                    else if (line.StartsWith(lengthPrefix, StringComparison.Ordinal))
                    {
                        try
                        {
                            this.length = Int32.Parse(line.Remove(0, lengthPrefix.Length).Trim('\n',' '));
                        }
                        catch (ArgumentException e)
                        {
                            throw new InvalidMessageFormatException(e.Message);
                        }
                        catch (FormatException e)
                        {
                            throw new InvalidMessageFormatException(e.Message);
                        }

                        numfields++;

                    }
                    else if (line.StartsWith(contentPrefix, StringComparison.Ordinal))
                    {
                         string rest = line + "\n" + reader.ReadToEnd();
                         rest = rest.Remove(0, contentPrefix.Length).Trim('\n',' ');

                        if (rest.Length == this.length)
                            this.content = rest;
                        else if (rest.Length < this.length)
                            throw new InvalidMessageFormatException("Actual content length " + rest.Length + 
                            " is shorter then expected length " + this.length);
                        else
                            this.content = rest.Substring(0, this.length);

                        numfields++;
                    }
                }
            }

            if (numfields != 5)
            {
                throw new InvalidMessageFormatException("Number of fields in the given message is incorrect: " + numfields);
            }
        }

        public MessagePacket(string from, string to, string content)
        {
            this.from = from.Trim('\n',' ');
            this.to = to.Trim('\n',' ');
            this.msgSrc = MsgSrc.server; // has to be server;
            this.content = content.Trim('\n',' ');
            this.length = content.Length;
        }
        

        public byte[] Serialize()
        {
            string msgSrcstr;
            if (this.msgSrc == MsgSrc.server)
                msgSrcstr = "s\n";
            else
                msgSrcstr = "c\n";

            return Encoding.Unicode.GetBytes(starter + "\n" +
                                             fromPrefix + this.from + "\n" +
                                             toPrefix + this.to + "\n" +
                                             classPrefix + msgSrcstr + 
                                             lengthPrefix + this.length + "\n" +
                                             contentPrefix + "\n" + this.content + "\n" +
                                             ender);
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