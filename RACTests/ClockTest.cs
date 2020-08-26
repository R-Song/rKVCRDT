using System;
using Xunit;
using RAC;
using RAC.Errors;
using System.Text;
using System.Threading;

namespace RACTests
{   public class ClockTest
    {
        [Fact]
        public void TestCompareVector()
        {
            Clock c1 = new Clock(2, 0);
            Clock c2 = new Clock(2, 1);

            c1.Increment();

            Assert.Equal(1, c1.CompareVectorClock(c2));
            Assert.Equal(-1, c2.CompareVectorClock(c1));

            c2.Increment();

            Assert.Equal(0, c1.CompareVectorClock(c2));
        }

        [Fact]
        public void TestCompareWallClock()
        {
            Clock c1 = new Clock(2, 0);
            Clock c2 = new Clock(2, 1);

            Assert.Equal(0, c1.CompareWallClock(c2));
            
            Thread.Sleep(1000);
            c1.Increment();

            Assert.Equal(1, c1.CompareWallClock(c2));
            Assert.Equal(-1, c2.CompareWallClock(c1));

        }

        [Fact]
        public void TestMerge()
        {
            Clock c1 = new Clock(2, 0);
            Clock c2 = new Clock(2, 1);

            c1.Increment();

            Assert.Equal(-1, c2.CompareVectorClock(c1));

            c2.Merge(c1);
            c2.Increment();

            Assert.Equal(1, c2.CompareVectorClock(c1));
        }
        
        [Fact]
        public void TestMergeCompareError()
        {
            Clock c1 = new Clock(2, 0);
            Clock c2 = new Clock(3, 1);

            Assert.Throws<InvalidMessageFormatException>(() => c1.Merge(c2));
            Assert.Throws<InvalidMessageFormatException>(() => c1.CompareVectorClock(c2));
        }

        [Fact]
        public void StringConvertTest()
        {
            Clock c1 = new Clock(2, 0);
            Clock c2 = new Clock(2, 1);
            c1.Increment();
            c2.Increment();

            c1.Merge(c2);

            string str = c1.ToString();
            long wctime = c1.wallClockTime;

            Assert.Equal("0:2,1:" + wctime, str);
            Assert.Equal(0, Clock.FromString(str).CompareVectorClock(c1));
            Assert.Equal(0, Clock.FromString(str).CompareWallClock(c1));

        }

    }
}