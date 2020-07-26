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
            Console.WriteLine("successfully stored");
            Global.memoryManager.StorePayload(uid, payload);
        }

        public abstract Responses SetValue();
        public abstract Responses GetValue();
        public abstract Responses Synchronization();
        public virtual Responses DeleteValue()
        {
            Responses res = new Responses(Status.success);

            // TODD: deletion things

            return res;
        }

    }
    
}