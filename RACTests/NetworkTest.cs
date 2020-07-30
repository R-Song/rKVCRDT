using System;
using Xunit;
using RAC;
using RAC.Network;
using System.Text;

namespace RACTests
{   public class NetworkTest
    {
        [Fact]
        public void TestCreatePacketFromString()
        {
            string input = @"-RAC-
FROM:100.100.1.1:5000
TO:150.150.2.2:5001
CLS:c
LEN:8
CNT:
gc
1
i
5
-EOF-";
            MessagePacket msg = new MessagePacket(input);

            Assert.Equal("100.100.1.1:5000", msg.from) ;
            Assert.Equal("150.150.2.2:5001", msg.to);
            Assert.Equal(MsgSrc.client, msg.msgSrc);
            Assert.Equal(8, msg.length);
            Assert.Equal("gc\n1\ni\n5", msg.content);
        }

                public void TestCreatePacketFromDeformedString()
        {
            string input = @"-RAC-
FROM:100.100.1.1:5000
TO:150.150.2.2:5001
CLS:c
LEN:8



CNT:
gc
1
i
5



Randomstuffs
-EOF-";
            MessagePacket msg = new MessagePacket(input);

            Assert.Equal("100.100.1.1:5000", msg.from) ;
            Assert.Equal("150.150.2.2:5001", msg.to);
            Assert.Equal(MsgSrc.client, msg.msgSrc);
            Assert.Equal(8, msg.length);
            Assert.Equal("gc\n1\ni\n5\n", msg.content);
        }

        [Fact]
        public void TestCreateFromParams()
        {
            MessagePacket msg = new MessagePacket("100.100.1.1:5000", "150.150.2.2:5001", "gc\n1\ni\n5");

            Assert.Equal("100.100.1.1:5000", msg.from) ;
            Assert.Equal("150.150.2.2:5001", msg.to);
            Assert.Equal(MsgSrc.server, msg.msgSrc);
            Assert.Equal(8, msg.length);
            Assert.Equal("gc\n1\ni\n5", msg.content);
        }


        [Fact]
        public void TestCreatePacketFromStringWrongLength()
        {
            string input = @"-RAC-
FROM:100.100.1.1:5000
TO:150.150.2.2:5001
CLS:s
LEN:15
CNT:
gc
1
i
5
-EOF-";

            Assert.Throws<RAC.Errors.InvalidMessageFormatException>(() => new MessagePacket(input));
        }

        [Fact]
        public void TestCreatePacketMissingField()
        {
            string input = @"-RAC-
FROM:100.100.1.1:5000
TO:150.150.2.2:5001
LEN:15

gc
1
i
5
-EOF-";

            Assert.Throws<RAC.Errors.InvalidMessageFormatException>(() => new MessagePacket(input));
        }


        [Fact]
        public void TestSerialization()
        {
            string input = @"-RAC-
FROM:100.100.1.1:5000
TO:150.150.2.2:5001
CLS:s
LEN:8
CNT:
gc
1
i
5
-EOF-";
            MessagePacket msg = new MessagePacket(input);

            string b2 = Encoding.Unicode.GetString(msg.Serialize());

            Assert.Equal(msg.Serialize(), Encoding.Unicode.GetBytes(input));
            
        }

    
    }
}
