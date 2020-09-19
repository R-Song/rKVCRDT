using System;
using System.Collections.Generic;

namespace RAC.Payloads
{
    public class ORSetPayload : Payload
    {
        // todo: put any necessary data here
        public HashSet<(string value, string tag)> addSet;
        public HashSet<(string value, string tag)> removeSet;

        // used for generate unique tag
        public Random random { get; }

        public ORSetPayload(string uid)
        {
            this.uid = uid;
            this.addSet = new HashSet<(string value, string tag)>();
            this.removeSet = new HashSet<(string value, string tag)>();
            this.random = new Random();
        }

    }
}