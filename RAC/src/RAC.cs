using System;
using System.Collections.Generic;
using RAC.Network;

namespace RAC
{
    static class Config
    {
        public static int MAX_CORE = 2;
        public static int replicaId;
        public static int numReplicas;
    }

    static class Constants
    {

    }

    static class Global
    {
        public static MemoryManager memoryManager = new MemoryManager();
        public static Node selfNode;
        public static Cluster cluster;
        public static Server server;
        public static Profiler profiler;

        public static void init(string nodeconfigfile)
        {
            API.InitAPIs();

            cluster = new Cluster(nodeconfigfile);

            selfNode = cluster.selfNode;
            Config.numReplicas = cluster.numNodes;
            Config.replicaId = selfNode.nodeid;

            // set cpu cores
            if (Config.MAX_CORE > 0)
            {
                ulong cpu_affin = 0;
                int cores = System.Environment.ProcessorCount;

                for (int i = 0; i < Config.MAX_CORE; i++)
                {
                    cpu_affin |= (ulong)1 << (int)(cores - (selfNode.nodeid * Config.MAX_CORE) - i - 1);
                }
                
                System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity = (System.IntPtr)cpu_affin;
            }

            server = new Server(Global.selfNode);
            profiler = new Profiler();
        }
    }
}