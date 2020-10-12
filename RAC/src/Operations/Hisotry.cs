using System;
using RAC.Payloads;

namespace RAC.Operations
{
        public class HistoryHandler : Operation<Payload>
    {
        public HistoryHandler(string uid, Parameters parameters)  : base(uid, parameters)
        {

        }

        public override string typecode { get ; set; } = "h";

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
            history.Merge(parameters.GetParam<string>(0), string.GetParam<int>(1));
            return new Responses(Status.success);
        }
    }
}