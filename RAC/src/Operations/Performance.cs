using System;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Get value will return:
    /// Memory usage
    /// Data sent
    /// Data received
    /// ?
    /// </summary>
    public class PerformanceMonitor : Operation<Payload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "perf";

        public PerformanceMonitor(string uid, Parameters parameters) : base(uid, parameters)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            
            long mem = Global.profiler.GetCurrentMemUsage();
            var res = new Responses(Status.success);
            
            var report = string.Format(
@"
===Performance Report===
Current Memory Usage: {0}

"
            , mem / 1000000);

            res.AddResponse(Dest.client, report);
            return res;
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

