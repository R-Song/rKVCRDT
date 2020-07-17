using System.Collections.Generic;
using RAC.Payloads;

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

        // TODO: temp

        public static Dictionary<string, GCPayload> Gcountervec;

    }


}