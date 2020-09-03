using System;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    public class Template : Operation<TemplatePayload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "";

        public Template(string uid, Parameters parameters) : base(uid, parameters)
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

