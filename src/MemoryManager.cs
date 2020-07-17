using System.Collections.Generic;

using RAC.Payloads;

namespace RAC
{
    public class MemoryManager
    {
        // TODO: a better storage system
        public Dictionary<string, Payload> storage;

        public MemoryManager()
        {
            storage = new Dictionary<string, Payload>();
        }

        public bool StorePayload(string uid, Payload payload)
        {
            storage[uid] = payload;
            return true;
        }

        public Payload GetPaylod(string uid)
        {
            return storage[uid];
        }
    }
}