using System.Collections.Generic;

using RAC.Payloads;
using RAC.History;
using RAC.Errors;


namespace RAC
{
    public class MemoryManager
    {

        private Dictionary<string, Payload> storage;
        // TODO: make history private
        public Dictionary<string, OpHistory> history;

        public MemoryManager()
        {
            storage = new Dictionary<string, Payload>();
            history = new Dictionary<string, OpHistory>();
        }

        public bool StorePayload(string uid, Payload payload)
        {
            storage[uid] = payload;
            return true;
        }

        public Payload GetPayload(string uid)
        {
            try
            {
                return storage[uid];
            }
            catch (KeyNotFoundException)
            {
                throw new PayloadNotFoundException();
            }
        }
    }
}