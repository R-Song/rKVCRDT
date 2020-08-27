using System;
using System.Collections.Generic;
using System.Linq;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    public class RCounter : Operation<RCounterPayload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "rc";

        public RCounter(string uid, Parameters parameters, Clock clock = null) : base(uid, parameters, clock)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            Responses res;

            if (this.payload is null)
            {
                res = new Responses(Status.fail);
                res.AddReponse(Dest.client, "Rcounter with id {0} cannot be found");
            } 
            else
            {
                int pos = payload.PVector.Sum();
                int neg = payload.NVector.Sum();

                res = new Responses(Status.success);
                res.AddReponse(Dest.client, (pos - neg).ToString()); 
            }
            payloadNotChanged = true;
            
            return res;
        }

        public override Responses SetValue()
        {

            RCounterPayload pl = new RCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);

            int value = this.parameters.GetParam<int>(0);
            if (value >= 0)
                pl.PVector[pl.replicaid] = value;
            else
                pl.NVector[pl.replicaid] = -value;

            this.payload = pl;

            Responses res = GenerateSyncRes();
            res.AddReponse(Dest.client); 
            
            return res;
        }

        public Responses Increment()
        {
            this.payload.PVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            Responses res = GenerateSyncRes();
            res.AddReponse(Dest.client); 

            return res;

        }

        public Responses Decrement()
        {
            this.payload.NVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            Responses res = GenerateSyncRes();
            res.AddReponse(Dest.client); 

            return res;

        }

        public override Responses Synchronization()
        {
            Responses res;

            List<int> otherP = this.parameters.GetParam<List<int>>(0);
            List<int> otherN = this.parameters.GetParam<List<int>>(1);
            
            if (this.payload is null)
            {
                RCounterPayload pl = new RCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);
                this.payload = pl;
            }

            if (otherP.Count != otherN.Count || otherP.Count != payload.PVector.Count)
            {   
                res = new Responses(Status.fail);
                LOG("Sync failed for item: " + this.payload.replicaid);
                return res;
            }

            for (int i = 0; i < otherP.Count; i++)
            {
                this.payload.PVector[i] = Math.Max(this.payload.PVector[i], otherP[i]);
                this.payload.NVector[i] = Math.Max(this.payload.NVector[i], otherN[i]);
            }
            
            DEBUG("Sync successful, new value for " + this.uid + " is " +  
                    (this.payload.PVector.Sum() - this.payload.NVector.Sum()));
            
            res = new Responses(Status.success);

            return res;

        }

        private Responses GenerateSyncRes()
        {
            Responses res = new Responses(Status.success);

            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, this.payload.PVector);
            syncPm.AddParam(1, this.payload.NVector);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            
            res.AddReponse(Dest.broadcast, broadcast, false);
            return res;
        }

    }



}

