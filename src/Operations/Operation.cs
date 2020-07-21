using System;

using RAC.Payloads;
using RAC.Errors;

namespace RAC.Operations
{
    public abstract class Operation<PayloadType> where PayloadType: Payload
    {
        public string uid;
        public Parameters parameters;
        
        public PayloadType payload;

        // TODO: add a payload changed flag

        public Operation(string uid, Parameters parameters)
        {
            this.uid = uid;
            this.parameters = parameters;
 
            try
            {
                this.payload = (PayloadType) Global.memoryManager.GetPayload(uid);
            }
            catch (PayloadNotFoundException) 
            {
                this.payload = null;
            }
            
        }

        public void Save()
        {
            Console.WriteLine("succseefully stored");
            Global.memoryManager.StorePayload(uid, payload);
        }

        public abstract Response SetValue();
        public abstract Response GetValue();
        public abstract Response Synchronization();
        public virtual Response DeleteValue()
        {
            Response res = new Response(Status.success);

            // TODD: deletion things

            return res;
        }

    }
    
}