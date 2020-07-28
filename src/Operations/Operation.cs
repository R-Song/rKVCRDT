using System;

using RAC.Payloads;
using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    public abstract class Operation<PayloadType> where PayloadType: Payload
    {
        public string uid { get; }
        public Parameters parameters { protected set; get; }
        
        public PayloadType payload { protected set; get; }

        protected bool payloadNotChanged = false;

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
            if (!payloadNotChanged)
            {
                Global.memoryManager.StorePayload(uid, payload);
                LOG(uid + " successfully stored");
            }
        }

        public abstract Responses SetValue();
        public abstract Responses GetValue();
        public abstract Responses Synchronization();
        public virtual Responses DeleteValue()
        {
            Responses res = new Responses(Status.success);

            // TODO: deletion things

            return res;
        }

    }
    
}