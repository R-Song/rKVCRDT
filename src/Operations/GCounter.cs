using System;
using System.Linq;
using System.Collections;

using RAC.Payloads;
using System.Collections.Generic;

namespace RAC.Operations
{
    public class GCounter : Operation<GCPayload>
    {

        //public GCPayload payload;

        public GCounter(string uid, Parameters parameters) : base(uid, parameters)
        {
        }


        public override Response GetValue()
        {
            Response res = new Response(Status.success);
            
            res.AddContent(payload.valueVector.Sum().ToString(), Dest.client); 

            return res;
        }

        public override Response SetValue()
        {
            Response res = new Response(Status.success);

            GCPayload pl = new GCPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);

            pl.valueVector.Insert(pl.replicaid, this.parameters.GetParam<int>(0));

            this.payload = pl;
            
            return res;
        }

        public Response Increment()
        {
            this.payload.valueVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            Response res = new Response(Status.success);

            Parameters syncPm = new Parameters(1);
            syncPm.AddParam(0, this.payload.valueVector);

            string broadcast = Parser.BuildCommand("gc", "y", this.uid, syncPm);
            
            res.AddContent(broadcast, Dest.broadcast);

            return res;

        }

        public override Response Synchronization()
        {
            Response res = new Response(Status.success);

            List<int> otherState = this.parameters.GetParam<List<int>>(0);
            
            // TODO: add sanity check

            for (int i = 0; i < otherState.Count; i++)
            {
                this.payload.valueVector[i] = Math.Max(this.payload.valueVector[i], otherState[i]);
            }

            return res;

        }
    }


}