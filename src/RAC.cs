using System.Collections.Generic;
using RAC.Payloads;
using RAC.Network;

namespace RAC
{
    static class Config
    {
        public static uint replicaId;
        public static uint numReplicas;

    }

    static class Constants
    {

    }

    static class Global
    {
        public static MemoryManager memoryManager = new MemoryManager();

        public static Node selfNode;

        

    }


}