using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class GCPayload : Payload
    {
        public int replicaid;
        public List<int> valueVector {set; get;}

        public GCPayload(string uid, int numReplicas, int replicaid)
        {
            this.uid = uid;
            this.valueVector = new List<int>(numReplicas);
            this.replicaid = replicaid;

            for (int i = 0; i < valueVector.Capacity; i++)
            {
                valueVector.Insert(i, 0);
            }
        }

    }
}