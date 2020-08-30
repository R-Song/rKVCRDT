using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class RCounterPayload : Payload
    {
        public int replicaid;
        public List<int> PVector {set; get;}
        public List<int> NVector {set; get;}
        public Dictionary<string, (RCounterPayload, RCounterPayload)> OpHistory{ set; get; }
        public Clock clock { set; get; }

        public RCounterPayload(string uid, int numReplicas, int replicaid, Clock clock)
        {
            this.uid = uid;
            this.PVector = new List<int>(numReplicas);
            this.NVector = new List<int>(numReplicas);
            this.replicaid = replicaid;
            this.OpHistory = new Dictionary<string, (RCounterPayload, RCounterPayload)>();
            this.clock = clock;

            for (int i = 0; i < numReplicas; i++)
            {
                PVector.Insert(i, 0);
                NVector.Insert(i, 0);
            }
        }

        public RCounterPayload CloneValues()
        {
            // TODO: optimize
            RCounterPayload copy = new RCounterPayload(uid, this.PVector.Count, this.replicaid, this.clock);
            copy.PVector = new List<int>(this.PVector);
            copy.NVector = new List<int>(this.NVector);

            return copy;
        }
    }
}