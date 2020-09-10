using System;
using System.Collections.Generic;
using System.Linq;

namespace RAC.Payloads
{
    public class RCounterPayload : Payload
    {
        public int replicaid;
        public List<int> PVector {set; get;}
        public List<int> NVector {set; get;}
        public Dictionary<string, List<string>> relations;
        // TODO: change to hashset if performance bad
        public HashSet<string> tombstone;

        public RCounterPayload(string uid, int numReplicas, int replicaid)
        {
            this.uid = uid;
            this.PVector = new List<int>(numReplicas);
            this.NVector = new List<int>(numReplicas);
            this.replicaid = replicaid;
            this.relations = new Dictionary<string, List<string>>();
            this.tombstone = new HashSet<string>();

            for (int i = 0; i < numReplicas; i++)
            {
                PVector.Insert(i, 0);
                NVector.Insert(i, 0);
            }
        }

        public RCounterPayload CloneValues()
        {
            // TODO: optimize
            RCounterPayload copy = new RCounterPayload(uid, this.PVector.Count, this.replicaid);
            copy.PVector = new List<int>(this.PVector);
            copy.NVector = new List<int>(this.NVector);

            return copy;
        }


        public static string PayloadToStr(Payload pl)
        {   
            var rpl = (RCounterPayload) pl;
            string pvecstr =  String.Join(",", rpl.PVector.ToArray());
            string nvecstr =  String.Join(",", rpl.NVector.ToArray());
            return pvecstr + "||" + nvecstr;

        }

        public static Payload StrToPayload(string str)
        {
            RCounterPayload pl = new RCounterPayload("", (int)Config.numReplicas, (int)Config.replicaId);
            string pvecstr = str.Split("||")[0];
            string nvecstr = str.Split("||")[1];

            var plisttemp = new List<string>(pvecstr.Split(","));
            pl.PVector = plisttemp.Select(int.Parse).ToList();

            var nlisttemp = new List<string>(nvecstr.Split(","));
            pl.NVector = nlisttemp.Select(int.Parse).ToList();

            return pl;
        }

    }
}