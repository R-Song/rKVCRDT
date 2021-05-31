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
            this.noSideEffect = true;
        }


        // human readable version
        public override Responses GetValue()
        {
            
            long mem = Global.profiler.GetCurrentMemUsage();
            long peakmem = Profiler.peakMemUsage;
            int totalops = Profiler.clientOpsTotal;
            int succeedOps = Profiler.clientOpsSuccess;

            var res = new Responses(Status.success);
            
            var report = string.Format(
@"===Performance Report===
Current Memory Usage: {0}
Peak Memory Usage: {1}
Total Operation Succeeded: {2}
Total Operation Executed: {3}"
            , mem / 1000000, 
            peakmem / 1000000,
            succeedOps,
            totalops);

            res.AddResponse(Dest.client, report);
            return res;
        }

        // Shortend version
        public Responses GetValueShort()
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

