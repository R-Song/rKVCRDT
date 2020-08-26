using System;

namespace RAC
{
    /// <summary>
    /// A class that contains both vector clock and 
    /// wall clock of local machine
    /// </summary>
    public class Clock
    {
        public long wallClockTime { get; private set; }

        private int[] vector;
        private int replicaid;

        public Clock(int numReplica, int replicaid)
        {
            this.wallClockTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            this.vector = new int[numReplica];
            this.replicaid = replicaid;

            for (int i = 0; i < numReplica; i++)
            {   
                this.vector[i] = 0;
            }
        }

        public long UpdateWallClockTime()
        {
            this.wallClockTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return this.wallClockTime;
        }

        public void Increment()
		{
			this.vector[replicaid]++;
		}
        
        public void Merge(Clock other)
		{
			for(int i = 0; i < this.vector.Length; i++)
			{
				if (this.replicaid == i)
				{
                    Increment();
				}
				else
				{
					this.vector[i] = Math.Max(other.vector[i], this.vector[i]);
				}
			}
		}

        /// <summary>
        /// Compare this and another vector clock.
        /// </summary>
        /// <param name="other">Another clock</param>
        /// <returns>
        /// -1 as other happens after this
        /// 0 as concurrent
        /// 1 as this happens after other
        /// </returns>
        public int CompareVectorClock(Clock other)
        {
            bool thisLarger = false;
            bool otherLarger = false;

            for (int i = 0; i < this.vector.Length; i++)
            {
                if (this.vector[i] > other.vector[i])
                {
                    thisLarger = true;
                }
                else if (this.vector[i] < other.vector[i])
                {
                    otherLarger = true;
                }
            }

            if (thisLarger && !otherLarger)
                return 1;
            else if ((thisLarger && otherLarger) || (!thisLarger && !otherLarger))
                return 0;
            else //if (!thisLarger && otherLarger)
                return -1;
        }

        /// <summary>
        /// Compare this and another wall clock.
        /// </summary>
        /// <param name="other">Another clock</param>
        /// <returns>
        /// -1 as other happens after this
        /// 0 as concurrent
        /// 1 as this happens after other
        /// </returns>
        public int CompareWallClock(Clock Other)
        {
            if (this.wallClockTime > Other.wallClockTime)
                return 1;
            else if (this.wallClockTime == Other.wallClockTime)
                return 0;
            else
                return -1;

        }
        
    }

}