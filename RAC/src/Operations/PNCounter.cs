using System;
using System.Collections.Generic;
using System.Linq;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Useful things:
    /// Payload: this.payload
    /// Parameters: this.parameters
    ///     get a parameter: this.parameters.GetParam<T>(int index)
    ///     create new Parameters for broadcast: Parameters syncPm = new Parameters(int numparams);
    ///     add values to Paramters: syncPm.AddParam(int index, object value)
    ///     Create parameter string: Parser.BuildCommand(string typeCode, string apiCode, string uid, Parameters pm)
    /// Create new Response: Responses res = new Responses(Status status)
    ///     Add content to Response res.AddResponse(Dest dest, string content = "", bool includeStatus = true)
    /// Access op history: this.history
    /// </summary>
    public class PNCounter : Operation<PNCPayload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "pnc";

        public PNCounter(string uid, Parameters parameters) : base(uid, parameters)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            Responses res;

            if (this.payload is null)
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "Rcounter with id {0} cannot be found");
            } 
            else
            {
                int pos = payload.PVector.Sum();
                int neg = payload.NVector.Sum();

                
                res = new Responses(Status.success);
                res.AddResponse(Dest.client, (pos - neg).ToString()); 
                
            }
            return res;

        }

        public override Responses SetValue()
        {

            Responses res = new Responses(Status.success);
            PNCPayload pl = new PNCPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);

            int value = this.parameters.GetParam<int>(0);
            if (value >= 0)
                pl.PVector[pl.replicaid] = value;
            else
                pl.NVector[pl.replicaid] = -value;

            this.payload = pl;

            GenerateSyncRes(ref res);
            res.AddResponse(Dest.client); 
            
            return res;
        }

        public Responses Increment()
        {   
            this.payload.PVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client);
            GenerateSyncRes(ref res);
            return res;

        }

        public Responses Decrement()
        {            
            this.payload.NVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client); 
            GenerateSyncRes(ref res);

            return res;

        }

        private void GenerateSyncRes(ref Responses res)
        {
            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, this.payload.PVector);
            syncPm.AddParam(1, this.payload.NVector);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            
            res.AddResponse(Dest.broadcast, broadcast, false);
        }

        public override Responses Synchronization()
        {
            Responses res = new Responses(Status.success);

            List<int> otherP = this.parameters.GetParam<List<int>>(0);
            List<int> otherN = this.parameters.GetParam<List<int>>(1);
            
            if (this.payload is null)
            {
                PNCPayload pl = new PNCPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);
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
    }



}

