using RAC.Payloads;
using System.Collections.Generic;

namespace RAC.Operations
{
    public abstract class Operation
    {
        public string uid;
        public Payload payload;
        public List<string> parameters;

        public Operation(string uid, List<string> parameters)
        {
            this.uid = uid;
            this.parameters = parameters;
            this.payload = Global.memoryManager.GetPaylod(uid);
        }

        public abstract void SetValue(string uid, List<string> parameters);


    }
}