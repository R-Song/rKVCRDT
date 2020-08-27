using System;

using RAC.Payloads;
using RAC.Errors;
using static RAC.Errors.Log;

namespace RAC.Operations
{   
    /// <summary>
    /// Base class for all CRDT replication algorithms. 
    /// Also holds infomation on current operation.
    /// </summary>
    /// <typeparam name="PayloadType">Payload class of this CRDT</typeparam>
    public abstract class Operation<PayloadType> where PayloadType: Payload
    {
        public string uid { get; }
        public Clock clock { get; }
        public abstract string typecode { get; set; }

        public Parameters parameters { protected set; get; }

        /// <summary>
        /// Payload that actually holds the states of a CRDT.
        /// Other methods should use this attribute to gain access to the
        /// states of CRDT and set this attribtue when state is changed/updated.
        /// </summary>
        public PayloadType payload { protected set; get; }

        /// <summary>
        /// Used by Save(), if this set to true, the this.payload will not
        /// be saved by the Memory Manager for this operation.
        /// </summary>
        protected bool payloadNotChanged = false;

        /// <summary>
        /// Constructor:
        /// </summary>
        /// <param name="uid">uid of the accessing object for this op</param>
        /// <param name="parameters">any parameters passed in for this op</param>
        public Operation(string uid, Parameters parameters, Clock clock = null)
        {
            this.uid = uid;
            this.clock = clock;
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

        /// <summary>
        /// Call the memory manager to store the this.payload. 
        /// </summary>
        public void Save()
        {
            if (!payloadNotChanged)
            {
                Global.memoryManager.StorePayload(uid, payload);
                LOG(uid + " successfully stored");
            }
        }

        // =================================!IMPORTANT!===========================================
        // ====!!NEXT 3 METHODS MUST BE IMPLEMENTED AND PROVIDED AS APIs FOR ANY CRDT!!======

        // Request Handling APIs:
        /// <summary>
        /// Set the given value in this.parameters to CRDT object with the given uid.
        /// If uid DNE, create a new CRDT object.
        /// This method must be provided by any CRDT.
        /// </summary>
        /// <returns>A Responses instance containing any information to the client or
        /// broadcasting messages.</returns>
        public abstract Responses SetValue();

        /// <summary>
        /// Get the value of a CRDT object with given uid.
        /// This method must be provided by any CRDT.
        /// </summary>
        /// <returns>A Responses instance containing any information to the client or
        /// broadcasting messages.</returns>
        public abstract Responses GetValue();

        /// <summary>
        /// Synchronize the state of the object based on the the given values in 
        /// this.paramters. Normally this is a request come from other replicas.
        /// This method must be provided by any CRDT.
        /// </summary>
        /// <returns>A Responses instance containing any information to the client or
        /// broadcasting messages.</returns>
        public abstract Responses Synchronization();

        // ========================================================================================

        /// <summary>
        ///  
        /// </summary>
        /// <returns>A Responses instance containing any information to the client or
        /// broadcasting messages.</returns>
        public virtual Responses DeleteValue()
        {
            Responses res = new Responses(Status.success);

            // TODO: deletion things

            return res;
        }

    }
    
}