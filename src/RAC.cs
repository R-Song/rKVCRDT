using System.Collections.Generic;
using RAC.Payloads;
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

        public static List<Node> cluster;

        public static void init(string nodeconfigfile)
        {
            Node.DeserializeNodeConfig(nodeconfigfile, out cluster);
            foreach (var n in cluster)
            {
                if (n.isSelf)
                    selfNode = n;
            }

            Config.numReplicas = cluster.Count;
            Config.replicaId = selfNode.nodeid;

            API.InitAPIs();
        }

        

    }


}