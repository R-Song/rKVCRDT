using System;
using System.Collections.Generic;
using RAC.Payloads;

/// <summary>
/// These classes are for reversible CRDT.
/// For normal CRDT, this module does not needed to be included.
/// </summary>
namespace RAC.History
{
    public class HistoryEntry
    {
        public Payload before;
        public Payload after;
        public delegate string PayloadToStr<T>(T pl);
        public delegate T StringToPayload<T>(T pl);
        public Clock time;

        public HistoryEntry(Payload before, Payload after, Clock clock)
        {

        }

    }

    // history of each object
    public class ObjectHistory
    {   
        public string uid;
        public List<HistoryEntry> log;

        public ObjectHistory()
        {
            log = new List<HistoryEntry>();
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