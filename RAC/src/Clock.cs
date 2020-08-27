using System;
using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC
{
    /// <summary>
    /// A class that contains both vector clock and 
    /// wall clock of local machine
    /// </summary>
    public class Clock
    {
        public long wallClockTime { get; private set; }
        
        /// During merge, if wallclock is found to be behind other clock
        /// then set this value to the difference of other wall clock
        // private long wallClockTimeOffset = 0;

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
            UpdateWallClockTime();
		}
        
        public void Merge(Clock other)
		{

            if (this.vector.Length != other.vector.Length)
                ERROR("Invalid clock merge", new InvalidMessageFormatException());

			for(int i = 0; i < this.vector.Length; i++)
			{
				if (this.replicaid == i)
                    Increment();
				else
					this.vector[i] = Math.Max(other.vector[i], this.vector[i]);
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

            if (this.vector.Length != other.vector.Length)
                ERROR("Invalid clock comparison", new InvalidMessageFormatException());

            for (int i = 0; i < this.vector.Length; i++)
            {
                if (this.vector[i] > other.vector[i])
                    thisLarger = true;
                else if (this.vector[i] < other.vector[i])
                    otherLarger = true;
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

        public override string ToString() 
        {            
            string vectorstr = string.Join( ",", this.vector);
            return this.replicaid + ":" + vectorstr + ":" + this.wallClockTime;
        }

        public static Clock FromString(string str)
        {
            try
            {
                string[] tokens = str.Trim().Split(":");

                int rid = Int32.Parse(tokens[0]);
                string[] vectors = tokens[1].Split(",");

                long wallclock = Int64.Parse(tokens[2]);
                
                Clock ret = new Clock(vectors.Length, rid);

                for (int i = 0; i < vectors.Length; i++)
                {
                    ret.vector[i] = Int32.Parse(vectors[i]);
                }

                ret.wallClockTime = wallclock;

                return ret;
            }
            catch (System.Exception)
            {
                ERROR("Wrong clock format: " + str, new InvalidMessageFormatException());
                return null;
            }

        }
        
    }

}