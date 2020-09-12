using System.Collections.Generic;
using RAC.Network;

namespace RAC
{
    static class Config
    {
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

        public static void init(string nodeconfigfile)
        {
            API.InitAPIs();

            cluster = new Cluster(nodeconfigfile);

            selfNode = cluster.selfNode;
            Config.numReplicas = cluster.numNodes;
            Config.replicaId = selfNode.nodeid;

            server = new Server(Global.selfNode);
        }
    }
}