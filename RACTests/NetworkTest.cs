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
100.100.1.1:5000
150.150.2.2:5001
c
9
gc
1
i
5
RandomStuff
-EOF-";
            MessagePacket msg = new MessagePacket(input);

            Assert.Equal(msg.from, "100.100.1.1:5000");
            Assert.Equal(msg.to, "150.150.2.2:5001");
            Assert.Equal(msg.msgSrc, MsgSrc.client);
            Assert.Equal(msg.length, 9);
            Assert.Equal(msg.content, "gc\n1\ni\n5\n");
        }

        [Fact]
        public void TestCreatePacketFromStringWrongLength()
        {
            string input = @"-RAC-
100.100.1.1:5000
150.150.2.2:5001
c
15
gc
1
i
5
-EOF-";

            Assert.Throws<RAC.Errors.MessageLengthDoesNotMatchException>(() => new MessagePacket(input));
        }


        [Fact]
        public void TestSerialization()
        {
            string input = @"-RAC-
100.100.1.1:5000
150.150.2.2:5001
c
9
gc
1
i
5
-EOF-";
            MessagePacket msg = new MessagePacket(input);

            Assert.Equal(msg.Serialize(), Encoding.Unicode.GetBytes(input));

        }

    
    }
}
