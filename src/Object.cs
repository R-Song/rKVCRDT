using RAC;
using RAC.Operations;
using RAC.Payloads;

namespace RAC
{
    public abstract class Object
    {
        public string uid;
        public BaseType baseType;
        public ReplicationType replicationType;
        private IOperation operation;
        private Parser parser;
        private Payload payload;

        public Response HandleRequest(Request req)
        {
            Response res = new Response();

            return res;
        }

    }

}