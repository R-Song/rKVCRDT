using System;
using System.Collections.Generic;
using System.Linq;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Pure state-based reversible counter
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
                foreach (var tombed in this.payload.tombstone)
                {
                    Payload oldtemp;
                    Payload newtemp;
                    
                    history.GetEntry(tombed, RCounterPayload.StrToPayload, out oldtemp, out newtemp, out _);

                    RCounterPayload newstate = (RCounterPayload) newtemp;
                    RCounterPayload oldstate = (RCounterPayload) oldtemp;


                    int diff = (newstate.PVector.Sum() - newstate.NVector.Sum()) - 
                                (oldstate.PVector.Sum() - oldstate.NVector.Sum());

                    RCounterPayload pl = this.payload;

                    compensate += diff;

                }

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

            RelateTo(opid);

            GenerateSyncRes(ref res, opid + "||");
            res.AddResponse(Dest.client, opid); 
            
            return res;
        }

        public Responses Increment()
        {   
            Responses res = new Responses(Status.success);
            string related = this.parameters.GetParam<string>(1);
            
            if (!checkValid(related))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client);
                return res;
            }

            RCounterPayload oldstate = this.payload.CloneValues();
            this.payload.PVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = this.history.AddNewEntry(oldstate, this.payload, RCounterPayload.PayloadToStr);

            RelateTo(opid, related);

            GenerateSyncRes(ref res, opid + "||" + related);
            res.AddResponse(Dest.client, opid);
            return res;

        }

        public Responses Decrement()
        {
            Responses res = new Responses(Status.success);
            string related = this.parameters.GetParam<string>(1);
            
            if (!checkValid(related))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client);
                return res;
            }

            RCounterPayload oldstate = this.payload.CloneValues();
            this.payload.NVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            string opid = this.history.AddNewEntry(oldstate, this.payload, RCounterPayload.PayloadToStr);
            
            RelateTo(opid, related);

            GenerateSyncRes(ref res, opid + "||" + related);
            res.AddResponse(Dest.client, opid); 
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
            
            // add to relation
            string[] pm2 = this.parameters.GetParam<string>(2).Split("||");
            string opid = pm2[0].Trim();
            string relateTo = pm2[1].Trim();

            this.payload.relations[opid] = new List<string>();

            if (relateTo != "")
            {
                this.payload.relations[relateTo].Add(opid);
                
                // add to tombstone as needed       
                if (this.payload.tombstone.Contains(relateTo))
                    this.payload.tombstone.Add(opid);
            }
            
        
            res = new Responses(Status.success);

            return res;
        }

        public Responses SynchronizeTombstone()
        {
            // 1. add new tombstone
            // 2. check relations and put any necessary ops in relations 
            List<string> tombstoned = this.parameters.GetParam<List<string>>(0);
            foreach (string opid in tombstoned)
            {
                this.payload.tombstone.Add(opid);
                this.payload.tombstone.AddRange(this.payload.relations[opid]);
            }

            return new Responses(Status.success);
        }


        // if reverse ops without relation, just reverse that one
        // if reverse op with relation, do the selective reverse thing
        public Responses Reverse()
        {
            Responses res = new Responses(Status.success);
            string opid = this.parameters.GetParam<String>(0);
            
            // you cannot reverse a op that is already reversed
            if (!checkValid(opid))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client);
                return res;
            }

            // perpare

            // find related:
            List<string> related = this.payload.relations[opid];
            List<string> tombstoned = new List<string>();

            // check related
            foreach (string rid in related)
            {
                // do not reverse the ones has already been reversed
                if (!this.payload.tombstone.Contains(rid))
                {
                    // update relation map/tombstone info
                    this.payload.tombstone.Add(rid);
                    tombstoned.Add(rid);
                }

            }

            // reverse self
            this.payload.tombstone.Add(opid);
            tombstoned.Add(opid);

            // sync tombstone, since grow-only, 
            Parameters syncPm = new Parameters(1);
            syncPm.AddParam(0, tombstoned);
            string broadcast = Parser.BuildCommand(this.typecode, "yr", this.uid, syncPm);

            res.AddResponse(Dest.broadcast, broadcast, false);
            return res;
        }

        private void GenerateSyncRes(ref Responses res, string newop)
        {
            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, this.payload.PVector);
            syncPm.AddParam(1, this.payload.NVector);
            syncPm.AddParam(2, newop);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            
            res.AddResponse(Dest.broadcast, broadcast, false);
        }


        // check if the related op has been revered
        private bool checkValid(string related)
        {
            if (related == "")
                return true;

            return !this.payload.tombstone.Contains(related);

        }

        private void RelateTo(string opid, string related = "")
        {
            this.payload.relations[opid] = new List<string>();
            
            if (related == "")
                return;
            
            this.payload.relations[related].Add(opid);
        }

    }



}

