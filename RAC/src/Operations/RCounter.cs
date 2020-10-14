using System;
using System.Collections.Generic;
using System.Linq;
using RAC.History;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Reversible counter
    /// Reverse functions:
    ///  Causal reverse
    /// </summary>
    public class RCounter : Operation<RCounterPayload>
    {

        public override string typecode { get ; set; } = "rc";

        public RCounter(string uid, Parameters parameters) : base(uid, parameters)
        {
        }

        public override Responses GetValue()
        {   
            // 1. calculate initial value
            // 2. go to tombstone and retract everything
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
                int compensate = 0;

                // calculated ones been reversed
                foreach (var tombed in this.history.tombstone)
                {
                    string startime;
                    string endTime;
                    
                    history.GetEntry(tombed, out startime, out endTime, out _);

                    var toReversed = history.Search(Clock.FromString(startime), Clock.FromString(endTime));

                    foreach (var ops in toReversed)
                    {   
                        RCounterPayload newstate;
                        RCounterPayload oldstate;

                        history.GetEntry(ops, RCounterPayload.StrToPayload, out oldstate, out newstate, out _);

                        int diff = (newstate.PVector.Sum() - newstate.NVector.Sum()) - 
                                    (oldstate.PVector.Sum() - oldstate.NVector.Sum());

                        RCounterPayload pl = this.payload;

                        compensate -= diff;
                    }
                }

                DEBUG("actual value: " + (pos - neg).ToString() + " compensate: " + compensate);

                res = new Responses(Status.success);
                res.AddResponse(Dest.client, (pos - neg + compensate).ToString()); 
            }
            noSideEffect = true;
            
            return res;
        }

        public override Responses SetValue()
        {
            Responses res = new Responses(Status.success);
            RCounterPayload pl = new RCounterPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);
            RCounterPayload oldstate = pl.CloneValues();

            int value = this.parameters.GetParam<int>(0);
            if (value >= 0)
                pl.PVector[pl.replicaid] = value;
            else
                pl.NVector[pl.replicaid] = -value;

            this.payload = pl;
            string opid = this.history.AddNewEntry(oldstate, this.payload, RCounterPayload.PayloadToStr);

            GenerateSyncRes(ref res, opid);
            res.AddResponse(Dest.client, opid); 
            
            return res;
        }

        public Responses Increment()
        {   
            RCounterPayload oldstate = this.payload.CloneValues();
            this.payload.PVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = this.history.AddNewEntry(oldstate, this.payload, RCounterPayload.PayloadToStr);

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, opid);
            return res;

        }

        public Responses Decrement()
        {
            RCounterPayload oldstate = this.payload.CloneValues();
            this.payload.NVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = this.history.AddNewEntry(oldstate, this.payload, RCounterPayload.PayloadToStr);

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid); 
            GenerateSyncRes(ref res, opid);

            return res;

        }

        public override Responses Synchronization()
        {
            Responses res = new Responses(Status.success);

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


        public Responses Reverse()
        {
            Responses res = new Responses(Status.success);
            string opid = this.parameters.GetParam<String>(0);
            string startime = this.history.log[opid].time;
            string curTime = this.history.curTime.ToString(); 
            string reverseop = this.history.AddNewEntry(startime, curTime);
            
            this.history.addTombstone(reverseop);
            
            res.AddResponse(Dest.client);
            return res;
        }

        private void GenerateSyncRes(ref Responses res, string newop)
        {
            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, this.payload.PVector);
            syncPm.AddParam(1, this.payload.NVector);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            
            res.AddResponse(Dest.broadcast, broadcast, false);
        }

    }
}

