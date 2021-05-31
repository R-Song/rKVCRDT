
using System.Collections.Generic;
using System.Diagnostics;

namespace RAC
{
    public class Profiler
    {
        
        // Performance
        public static long peakMemUsage = 0;

        // client ops
        public static int clientOpsTotal = 0;
        public static int clientOpsSuccess = 0;

        // Network Stats
        public static long numReqReceived = 0;
        public static long numReqSent = 0;
        public static long numBytesReceived = 0;
        public static long numBytesSent = 0;

        // Set this to > 0 to start profile in seconds
        public int probeInterval;
        public List<int> MemoryUsageOverTime;

        public Profiler(int interval = 0)
        {
            this.probeInterval = interval;
            this.MemoryUsageOverTime = new List<int>();
        }

        /// <summary>
        /// Well gather the performance metrics
        /// </summary>
        public void Profile()
        {   

        }

        public long GetCurrentMemUsage()
        {
            var currentProcess = Process.GetCurrentProcess();
            long totalBytesOfMemoryUsed = currentProcess.WorkingSet64;
            if (totalBytesOfMemoryUsed > peakMemUsage)
                peakMemUsage = totalBytesOfMemoryUsed;

            return totalBytesOfMemoryUsed;
        }
        
        public (int total, int succeed) GetNumberOfOps()
        {
            return (clientOpsTotal, clientOpsSuccess);
        }

    }
    

}