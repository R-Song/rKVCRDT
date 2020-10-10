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

        public void GetEntry(string opid, StringToPayloadDelegate stringToPayload, out Payload before, out Payload after, out Clock time)
        {
            OpEntry item = this.log[opid];
            before = stringToPayload(item.before);
            after = stringToPayload(item.after);
            time = Clock.FromString(item.time);
        }

        public void Merge(string otherop)
        {
            DEBUG("Merging new op " + otherop);
            OpEntry newop = JsonConvert.DeserializeObject<OpEntry>(otherop);
            Clock newtime = Clock.FromString(newop.time);
            curTime.Merge(newtime);
            this.log[newop.opid] = newop;
            
        }

        public void addTombstone(string opid)
        {
            tombstone.Add(log[opid]);
        }

        public void addTombstone(string[] opids)
        {
            foreach (var opid in opids)
            {
                tombstone.Add(log[opid]);       
            }
        }

        // TODO: make this pretty
        /// <summary>
        /// Called on every update of CRDT OP to 
        /// synchronize the history
        /// </summary>
        /// <param name="newop"></param>
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