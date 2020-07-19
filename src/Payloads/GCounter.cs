using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class GCPayload : Payload
    {
        private uint numNodes;
        public List<int> valueVector {set; get;}

        public GCPayload(string uid, uint numReplicas)
        {
            this.uid = uid;
            this.valueVector = new List<int>((int) numReplicas);
        }

    }
}