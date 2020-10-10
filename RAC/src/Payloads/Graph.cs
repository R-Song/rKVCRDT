using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class GraphPayload : Payload
    {
        // todo: put any necessary data here
        public int value = 0;
        public HashSet<(string value, string tag)> vertices;
        public HashSet<((string v1, string v2), string tag)> edges;
         public Random random { get; }


        public GraphPayload(string uid)
        {
            this.uid = uid;
            this.vertices = new HashSet<(string value, string tag)>();
            this.edges = new HashSet<((string v1, string v2), string tag)>();
            this.random = new Random();
        }

    }
}