using System;
using System.Collections.Generic;
using RAC.Payloads;

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
        public string opid;
        public string before;
        public string after;
        public string time;

        public OpEntry(string opid, string before, string after, string time)
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

            log.Add(opid, new OpEntry(opid, payloadToStr(before), payloadToStr(after), time.ToString()));

            return opid;
        }

        public void GetEntry(string opid, StringToPayloadDelegate stringToPayload, out Payload before, out Payload after, out Clock time)
        {
            OpEntry item = this.log[opid];
            before = stringToPayload(item.before);
            after = stringToPayload(item.after);
            time = Clock.FromString(item.time);
        }

        public void Merge()
        {

        }

        public void Sync()
        {

        }
    }

    public class OpHisotrys
    {
        // TODO: remove the class
        Dictionary<string, ObjectHistory> OpHisotry = new Dictionary<string, ObjectHistory>();


    }
}