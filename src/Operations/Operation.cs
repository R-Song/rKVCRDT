using RAC.Payloads;
using System.Collections.Generic;

namespace RAC.Operations
{
    public abstract class Operation
    {
        public string uid;
        public Parameters parameters;
        public Payload payload;
        

        public Operation(string uid, Parameters parameters)
        {
            this.uid = uid;
            this.parameters = parameters;
            this.payload = Global.memoryManager.GetPaylod(uid);
        }

        public abstract Response SetValue(Parameters parameters);
        public abstract Response GetValue();
        public virtual Response DelelteValue()
        {
            Response res = new Response();

            // TODD: deletion things

            return res;
        }

    }
}