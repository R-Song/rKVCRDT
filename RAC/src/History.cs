
using System;
using System.Collections.Generic;
using RAC.Payloads;
using RAC.Operations;
using Newtonsoft.Json;
using static RAC.Errors.Log;
using System.Linq;


/// <summary>
/// These classes are for reversible CRDT.
/// For normal CRDT, this module does not needed to be included.
/// </summary>
namespace RAC.History
{
    public delegate string PayloadToStrDelegate(Payload pl);
    public delegate T StringToPayloadDelegate<T>(string str);
    
    public class StateHisotryEntry
    {
        public string nodeid;
        public string opid;
        public string before;
        public string after;
        public string time;
        public HashSet<string> related;
        // use to mark if this op is a reverse op
        public bool isrev = false;

        // graph pointers
        public List<String> aft;
        // prev used for sync'd ops to link
        public List<String> prev;

        public StateHisotryEntry(string uid, string opid, string before, string after, string time, bool isrev)
        {   
            this.opid = opid;
            this.before = before;
            this.after = after;
            this.time = time;
            this.related = new HashSet<string>();
            this.aft = new List<string>();
            this.prev = new List<string>();
            this.isrev = isrev;
        }
    }
    

    // history of each object
    public class OpHistory
    {   
        public string uid;
        public Dictionary<string, StateHisotryEntry> log;

        // heads of the graph
        public List<string> heads;
        // can be used tombstone reverse
        public List<string> tombstone;

        public Clock curTime;

        public OpHistory(string uid)
        {
            this.uid = uid;
            this.log = new Dictionary<string, StateHisotryEntry>();
            this.heads = new List<string>();
            this.tombstone = new List<string>();
            this.curTime = new Clock(Config.numReplicas, Config.replicaId);
        }

        /// <summary>
        /// Add an entry.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="payloadToStr"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public string AddNewEntry(Payload before, Payload after, PayloadToStrDelegate payloadToStr, bool rev = false, Clock time = null)
        {
            string beforestr = payloadToStr(before);
            string afterstr = payloadToStr(after);

            return AddNewEntry(beforestr, afterstr, rev, time);
        }
        
        /// <summary>
        /// Add an entry, but states already in strings.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public string AddNewEntry(string before, string after, bool rev = false, Clock time = null)
        {
            var opid = InsertEntry(before, after, rev);
            return opid;
        }

        private string InsertEntry(string before, string after, bool rev, Clock time = null)
        {
            if (time is null)   
                time = this.curTime;

            string opid = time.ToString();

            StateHisotryEntry newEntry = new StateHisotryEntry(this.uid, opid, before, after, time.ToString(), rev);
            this.log.Add(opid, newEntry);

            foreach (var i in this.heads)
            {
                // link the new one and old ones, may converging the states
                newEntry.prev.Add(i);
                this.log[i].aft.Add(opid);
            }

            // reset head
            this.heads.Clear();
            this.heads.Add(opid);
            
            
            Sync(newEntry);

            time.Increment();
            this.curTime = time;
            return opid;
        }

        /// <summary>
        /// Get an entry and cast to a Payload object.
        /// </summary>
        /// <param name="opid"></param>
        /// <param name="stringToPayload"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="time"></param>
        /// <typeparam name="T"></typeparam>
        public void GetEntry<T>(string opid, StringToPayloadDelegate<T> stringToPayload, out T before, out T after, out Clock time)
        {
            StateHisotryEntry item = this.log[opid];
            before = stringToPayload(item.before);
            after = stringToPayload(item.after);
            time = Clock.FromString(item.time);
        }
        
        /// <summary>
        /// Get an entry in string form.
        /// </summary>
        /// <param name="opid"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="time"></param>
        public void GetEntry(string opid, out string before, out string after, out Clock time)
        {
            StateHisotryEntry item = this.log[opid];
            before = item.before;
            after = item.after;
            time = Clock.FromString(item.time);
        }

        /// <summary>
        /// Handles received op
        /// </summary>
        /// <param name="otherop"></param>
        /// <param name="status"></param>
        public void Merge(string otherop, int status)
        {
            if (status == 0)
            {
                StateHisotryEntry op = JsonConvert.DeserializeObject<StateHisotryEntry>(otherop);
                DEBUG("Merging new op " + otherop);
                Clock newtime = Clock.FromString(op.time);
                curTime.Merge(newtime);

                // if an empty place holder already created to hold
                // the pointers, update that
                string opid = op.opid;
                if (this.log.ContainsKey(opid))
                    op.prev.AddRange(this.log[opid].prev);

                this.log[op.opid] = op;

                // update the pointers
                foreach (var i in op.prev)
                {   
                    // in case prev op is not sync'd yet, create a placeholder, see above also
                    if (!this.log.ContainsKey(i))
                    {
                        StateHisotryEntry newEntry = new StateHisotryEntry(this.uid, i, "", "", "", false);
                        this.log[i] = newEntry;
                    }
                        
                    this.log[i].aft.Add(opid);
                }

            }
            else if (status == 1)
            {
                DEBUG("Merging tombstone op " + otherop);
                this.tombstone.Add(otherop);
            }
            else if (status == 2)
            {
                StateHisotryEntry op = JsonConvert.DeserializeObject<StateHisotryEntry>(otherop);
                DEBUG("Merging new related op " + otherop);

                foreach (var r in op.related)
                {
                    // hashset automatically remove duplicate
                    this.log[op.opid].related.Add(r);   
                }

            }
        }

        public void addTombstone(string opid)
        {
            tombstone.Add(opid);
            Sync(opid, 1);
        }

        public void addTombstone(string[] opids)
        {
            foreach (var opid in opids)
            {
                addTombstone(opid);       
            }
        }

        /// <summary>
        /// Called on every update of CRDT OP to 
        /// synchronize the history
        /// </summary>
        /// <param name="newop"></param>
        /// <param name="status">0 = op, 1 = tombstone, 2 = related</param>
        public void Sync(StateHisotryEntry newop, int status = 0)
        {
            DEBUG("Syncing new op " + newop.opid);
            string json = JsonConvert.SerializeObject(newop, Formatting.Indented);
            
            Responses res = new Responses(Status.success);
            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, json);
            syncPm.AddParam(1, status);
            string broadcast = Parser.BuildCommand("h", "y", this.uid, syncPm);

            res.AddResponse(Dest.broadcast, broadcast, false);
            Global.server.StageResponse(res);

        }   

        public void Sync(string newop, int status = 0)
        {
            DEBUG("Syncing new op " + newop);
            
            Responses res = new Responses(Status.success);
            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, newop);
            syncPm.AddParam(1, status);
            string broadcast = Parser.BuildCommand("h", "y", this.uid, syncPm);

            res.AddResponse(Dest.broadcast, broadcast, false);
            Global.server.StageResponse(res);

        }   

        /// <summary>
        /// Search through ops happens between startop and endop time
        // can be used by CRDT OPs
        /// </summary>
        /// <param name="startime"></param>
        /// <param name="endtime"></param>
        /// <returns>opids of found ops</returns>
        public List<string> CasualSearch(Clock starttime, Clock endtime)
        {   
            List<string> res = new List<string>();

            //linear search
            foreach (var item in this.log)
            {
                StateHisotryEntry op = item.Value;
                Clock optime = Clock.FromString(op.time);
                
                // op exactly the same as start date,
                // after start time,
                // and before/concurrent of endtime
                if ((optime.CompareVectorClock(starttime) == 1 && optime.CompareVectorClock(endtime) < 1) ||
                    (optime.ToString().Equals(starttime.ToString())))
                    res.Add(op.opid);
            }
            return res;


        }

        /// <summary>
        /// Search through ops happens between startop and endop time
        //  can be used by CRDT OPs
        /// </summary>
        /// <param name="starttime"></param>
        /// <param name="endtime"></param>
        /// <returns>opids of found ops</returns>
        public List<string> CasualSearch(string starttime, string endtime)
        {
            
            List<string> res = new List<string>();
            Clock st = Clock.FromString(starttime);
            Clock et = Clock.FromString(endtime);

            // BFS from start
            Queue<string> Q = new Queue<string>();
            Q.Enqueue(starttime);

            List<string> concurrents = new List<string>();

            while (Q.Count > 0)
            {
                string opid = Q.Dequeue();

                StateHisotryEntry op = this.log[opid];
                Clock optime = Clock.FromString(op.time);                

                if ((optime.CompareVectorClock(st) == 1 && optime.CompareVectorClock(et) < 1) ||
                    (optime.ToString().Equals(starttime)))
                {

                    // a new level (BFS)
                    if (!op.isrev)
                    {
                        if (concurrents.Count == 0)
                            concurrents.Add(opid);
                        else
                        {
                            // if concurrent with other op in concurrents
                            if (optime.CompareVectorClock(Clock.FromString(this.log[concurrents[0]].time)) == 0)
                            {
                                concurrents.Add(opid);
                            }
                            else // next level
                            {
                                res.AddRange(ResolveConcurrent(concurrents));
                                concurrents.Clear();
                                concurrents.Add(opid);
                            }

                        }
                    }

                    foreach (var i in op.aft)
                    {
                        if (!Q.Contains(i))
                            Q.Enqueue(i);
                    }
                }
            }
            
            res.AddRange(ResolveConcurrent(concurrents));

            return res;
        }
        
        private List<string> ResolveConcurrent(List<string> concurrents)
        {
            // TODO: check the concurrent ones
            if (concurrents.Count == 0)
                return concurrents;

            return concurrents;
        }


        /// <summary>
        /// Add a related op's opid to given opid
        /// </summary>
        /// <param name="opid"></param>
        /// <param name="related"></param>
        public void addRelated(string opid, string related)
        {
            this.log[opid].related.Add(related);
            Sync(this.log[opid], 2);
        }

        /// <summary>
        /// Search related ops to given opid
        /// </summary>
        /// <param name="opid"></param>
        /// <returns>opids of found ops</returns>
        public List<string> Related(string opid)
        {
            List<string> res = new List<string>();
            Stack<string> toSearch = new Stack<string>();
            toSearch.Push(opid);
            while (toSearch.Count > 0)
            {
                string toadd = toSearch.Pop();
                res.Add(toadd);
                foreach (var item in this.log[toadd].related)
                    toSearch.Push(item);
            }

            return res;
        }
    }

    public class OpHistoryEager : OpHistory
    {

        public delegate void CompensateMethod(string opid);
        public CompensateMethod Compensate;

        public OpHistoryEager(string uid, CompensateMethod compensate) : base(uid)
        {      
            this.Compensate = compensate;
        }

        /// <summary>
        /// still overrides to keep things consistent
        /// </summary>
        /// <param name="opid"></param>
        public new void addTombstone(string opid)
        {
            addTombstone(opid, true);
        }

        /// <summary>
        /// Also applying compenstation while revresing
        /// </summary>
        /// <param name="opid"></param>
        public void addTombstone(string opid, bool sync)
        {
            tombstone.Add(opid);
            if (sync)
                Sync(opid, 1);

            // reverse things
            string starttime;
            string endtime;
            
            this.GetEntry(opid, out starttime, out endtime, out _);

            List<String> toReversed = this.CasualSearch(starttime, endtime);

            DEBUG("Compenstating eagerly");

            foreach (var ops in toReversed)
            {   
                this.Compensate(ops);
            }
        }

        public new void Merge(string otherop, int status)
        {
            if (status == 0)
            {
                StateHisotryEntry op = JsonConvert.DeserializeObject<StateHisotryEntry>(otherop);
                DEBUG("Merging new op " + otherop);
                Clock optime = Clock.FromString(op.time);
                curTime.Merge(optime);

                // if an empty place holder already created to hold
                // the pointers, update that
                string opid = op.opid;
                if (this.log.ContainsKey(opid))
                    op.prev.AddRange(this.log[opid].prev);



                this.log[op.opid] = op;

                // update the pointers
                foreach (var i in op.prev)
                {   
                    // in case prev op is not sync'd yet, create a placeholder, see above also
                    if (!this.log.ContainsKey(i))
                    {
                        StateHisotryEntry newEntry = new StateHisotryEntry(this.uid, i, "", "", "", true);
                        this.log[i] = newEntry;
                    }
                        
                    this.log[i].aft.Add(opid);
                }

                // check if reversing needed
                // calculated ones been reversed
                foreach (var tombed in this.tombstone)
                {
                    string starttime;
                    string endtime;
                    
                    this.GetEntry(tombed, out starttime, out endtime, out _);

                    Clock st = Clock.FromString(starttime);
                    Clock et = Clock.FromString(endtime);

                   if ((optime.CompareVectorClock(st) == 1 && optime.CompareVectorClock(et) < 1) || 
                        (optime.ToString().Equals(starttime)))    
                    {
                        this.Compensate(opid); 
                        break;
                    }
                   
                }

            }
            else if (status == 1)
            {
                DEBUG("Merging tombstone op " + otherop);
                this.addTombstone(otherop, false);
            }
            else if (status == 2)
            {
                StateHisotryEntry op = JsonConvert.DeserializeObject<StateHisotryEntry>(otherop);
                DEBUG("Merging new related op " + otherop);

                foreach (var r in op.related)
                {
                    // hashset automatically remove duplicate
                    this.log[op.opid].related.Add(r);   
                }

            }

        }
    }
}