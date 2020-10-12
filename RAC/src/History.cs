using System;
using System.Collections.Generic;
using RAC.Payloads;
using RAC.Operations;
using Newtonsoft.Json;
using static RAC.Errors.Log;

/// <summary>
/// These classes are for reversible CRDT.
/// For normal CRDT, this module does not needed to be included.
/// </summary>
namespace RAC.History
{
    public delegate string PayloadToStrDelegate(Payload pl);
    public delegate Payload StringToPayloadDelegate(string str);
    
    public class OpEntry
    {
        public string uid;
        public string opid;
        public string before;
        public string after;
        public string time;
        public HashSet<string> related;

        public OpEntry(string uid, string opid, string before, string after, string time)
        {
            this.opid = opid;
            this.before = before;
            this.after = after;
            this.time = time;
            this.related = new HashSet<string>();
        }
    }

    // history of each object
    public class ObjectHistory
    {   
        public string uid;
        public Dictionary<string, OpEntry> log;
        // can be used tombstone reverse
        public List<OpEntry> tombstone;

        public Clock curTime;

        public ObjectHistory(string uid)
        {
            this.uid = uid;
            log = new Dictionary<string, OpEntry>();
            curTime = new Clock(Config.numReplicas, Config.replicaId);
        }

        /// <summary>
        /// Add an entry.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="payloadToStr"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public string AddNewEntry(Payload before, Payload after, PayloadToStrDelegate payloadToStr, Clock time = null)
        {
            if (time is null)   
                time = curTime;

            string opid = Config.replicaId + ":" + time.ToString();
            time.Increment();
            OpEntry newEntry = new OpEntry(this.uid, opid, payloadToStr(before), payloadToStr(after), time.ToString());
            log.Add(opid, newEntry);
            
            Sync(newEntry);

            return opid;
        }
        
        /// <summary>
        /// Add an entry, but states already in strings.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public string AddNewEntry(string before, string after, Clock time = null)
        {
            if (time is null)   
                time = curTime;

            string opid = Config.replicaId + ":" + time.ToString();
            time.Increment();
            OpEntry newEntry = new OpEntry(this.uid, opid, before, after, time.ToString());
            log.Add(opid, newEntry);
            
            Sync(newEntry);

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
        public void GetEntry(string opid, StringToPayloadDelegate stringToPayload, out Payload before, out Payload after, out Clock time)
        {
            OpEntry item = this.log[opid];
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
            OpEntry item = this.log[opid];
            before = item.before;
            after = item.after;
            time = Clock.FromString(item.time);
        }

        public void Merge(string otherop, int status)
        {
            
            OpEntry op = JsonConvert.DeserializeObject<OpEntry>(otherop);

            if (status == 0)
            {
                DEBUG("Merging new op " + otherop);
                Clock newtime = Clock.FromString(op.time);
                curTime.Merge(newtime);
                this.log[op.opid] = op;
            }
            else if (status == 1)
            {
                DEBUG("Merging tombstone op " + otherop);
                this.tombstone.Add(op);
            }
            
        }

        public void addTombstone(string opid)
        {
            tombstone.Add(log[opid]);
            Sync(log[opid], 1);
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
        /// <param name="status">0 = op, 1 = tombstone</param>
        public void Sync(OpEntry newop, int status = 0)
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

        /// <summary>
        /// Search through ops happens between startop and endop time
        // can be used by CRDT OPs
        /// </summary>
        /// <param name="startime"></param>
        /// <param name="endtime"></param>
        /// <returns>opids of found ops</returns>
        public List<string> Search(Clock startime, Clock endtime)
        {   
            List<string> res = new List<string>();

            // linear search
            foreach (var item in this.log)
            {
                OpEntry op = item.Value;
                Clock optime = Clock.FromString(op.time);
                
                // op after start time, and before/concurrent of endtime
                if (optime.CompareVectorClock(startime) == 1 && optime.CompareVectorClock(endtime) < 1)
                    res.Add(op.uid);
            }
            return res;
        }

        /// <summary>
        /// Add a related op's opid to given opid
        /// </summary>
        /// <param name="opid"></param>
        /// <param name="related"></param>
        public void addRelated(string opid, string related)
        {
            this.log[opid].related.Add(related);

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

    /*
    public class CausalHistory
    {   
                
        public string uid;
        public Clock curTime;

        // used to keep track of all vertices
        // opid - each op is a vertex
        public Dictionary<string, OpEntry> vertices; 
        // opid - opid to represent a edge
        public Dictionary<string, string> edges;
        // tails to add to
        OpEntry tail;

        public CausalHistory(string uid)
        {
            this.uid = uid;
            log = new Dictionary<string, OpEntry>();
            curTime = new Clock(Config.numReplicas, Config.replicaId);
            tail = null;
        }

        public void GetEntry(string opid, StringToPayloadDelegate stringToPayload, out Payload before, out Payload after, out Clock time)
        {
            OpEntry item = this.log[opid];
            before = stringToPayload(item.before);
            after = stringToPayload(item.after);
            time = Clock.FromString(item.time);
        }

        public string AddNewEntry(Payload before, Payload after, PayloadToStrDelegate payloadToStr, Clock time = null, string relatedid = null)
        {
            if (time is null)   
                time = curTime;

            string opid = Config.replicaId + ":" + time.ToString();
            time.Increment();
            OpEntry newEntry = new OpEntry(this.uid, opid, payloadToStr(before), payloadToStr(after), time.ToString());
            log.Add(opid, newEntry);

            if (tail != null)
            {
                tail.adjacency.Add(newEntry.uid);
                Sync(tail);
            }

            if (relatedid != null)
            {
                // TODO: maybe some checks here
                log[relatedid].related.Add(newEntry.uid);
                Sync(log[relatedid]);
            }

            Sync(newEntry);
            
            tail = newEntry;

            return opid;

        }

        public void Sync(OpEntry newop)
        {
            DEBUG("Syncing new op " + newop.opid);
            string json = JsonConvert.SerializeObject(newop, Formatting.Indented);
            
            Responses res = new Responses(Status.success);
            Parameters syncPm = new Parameters(1);
            syncPm.AddParam(0, json);
            string broadcast = Parser.BuildCommand("h", "y", this.uid, syncPm);
            res.AddResponse(Dest.broadcast, broadcast, false);
            
            Global.server.StageResponse(res);

        }   

        public void Merge(string otherop)
        {
            DEBUG("Merging op " + otherop);
            OpEntry newop = JsonConvert.DeserializeObject<OpEntry>(otherop);
            Clock newtime = Clock.FromString(newop.time);
            curTime.Merge(newtime);
            this.log[newop.opid] = newop;
            
            // TODO: things

            
        }

        public void Search(string startop, string endop)
        {

        }

        public void Related(string op)
        {

        }

   
    } */

}