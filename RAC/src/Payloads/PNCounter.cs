using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class PNCPayload : Payload
    {
        // todo: put any necessary data here
        public int replicaid;
        public List<int> PVector {set; get;}
        public List<int> NVector {set; get;}

        public PNCPayload(string uid, int numReplicas, int replicaid)
        {
            this.uid = uid;
            this.PVector = new List<int>(numReplicas);
            this.NVector = new List<int>(numReplicas);
            this.replicaid = replicaid;

            for (int i = 0; i < numReplicas; i++)
            {
                PVector.Insert(i, 0);
                NVector.Insert(i, 0);
            }
        }

    }
}