using System;
using System.Collections.Generic;
using RAC.Payloads;
using Newtonsoft.Json;
using RAC.Network;

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

        public OpEntry(string uid, string opid, string before, string after, string time)
        {
            this.opid = opid;
            this.before = before;
            this.after = after;
            this.time = time;
        }
    }

    // history of each object
    public class ObjectHistory
    {   
        public string uid;
        public Dictionary<string, OpEntry> log;

        public Clock curTime;

        public ObjectHistory()
        {
            log = new Dictionary<string, OpEntry>();
            curTime = new Clock(Config.numReplicas, Config.replicaId);
        }

        public string AddNewEntry(Payload before, Payload after, PayloadToStrDelegate payloadToStr, Clock time = null)
        {
            if (time is null)   
                time = curTime;

            string opid = Config.replicaId + ":" + time.ToString();

            log.Add(opid, new OpEntry(this.uid, opid, payloadToStr(before), payloadToStr(after), time.ToString()));

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
            OpEntry newop = JsonConvert.DeserializeObject<OpEntry>(otherop);
            Clock newtime = Clock.FromString(newop.time);
            curTime.Merge(newtime);
            this.log.Add(newop.opid, newop);
        }

        public void Sync(OpEntry newop)
        {
            string json = JsonConvert.SerializeObject(newop, Formatting.Indented);
            
            Responses res = new Responses(Status.success);
            Parameters syncPm = new Parameters(1);
            string broadcast = Parser.BuildCommand("h", "y", this.uid, syncPm);
            res.AddResponse(Dest.broadcast, broadcast, false);
            
            Global.selfNode.

        }   
    }

}