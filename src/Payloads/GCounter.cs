using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    class GCPayload : Payload
    {
        private uint numNodes;
        public List<int> valueVector {set; get;}

        public GCPayload(string uid, uint numNodes)
        {
            this.uid = uid;
            this.valueVector = new List<int>((int) numNodes);
        }

    }
}