using System;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    public class RCounter : Operation<RCounterPayload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "rc";

        public RCounter(string uid, Parameters parameters, Clock clock = null) : base(uid, parameters, clock)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            throw new NotImplementedException();
        }

        public override Responses SetValue()
        {
            throw new NotImplementedException();
        }

        public override Responses Synchronization()
        {
            throw new NotImplementedException();
        }
    }



}

