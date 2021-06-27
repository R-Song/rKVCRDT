using System;
using RAC.Payloads;
using static RAC.Errors.Log;

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
            history.Merge(parameters.GetParam<string>(0), parameters.GetParam<int>(1), parameters.GetParam<int>(2));
            return new Responses(Status.success);
        }

        public new void Save()
        {
        }
    }
}