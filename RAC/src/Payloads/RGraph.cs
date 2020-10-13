using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class RGraphPayload : Payload
    {
        // todo: put any necessary data here
        public int value = 0;
        public HashSet<(string value, string tag)> vertices;
        // {vertices : opid} map, used for adding relation when adding edges
        public Dictionary<string, string> vaddops;
        public HashSet<((string v1, string v2), string tag)> edges;
         public Random random { get; }


        public RGraphPayload(string uid)
        {
            this.uid = uid;
            this.vertices = new HashSet<(string value, string tag)>();
            this.vaddops = new Dictionary<string, string>();
            this.edges = new HashSet<((string v1, string v2), string tag)>();
            this.random = new Random();
        }

    }
}