using System.Linq;
using System.Collections;

using RAC.Payloads;



namespace RAC.Operations
{
    public class GCounter : Operation<GCPayload>
    {

        //public GCPayload payload;

        public GCounter(string uid, Parameters parameters) : base(uid, parameters)
        {
        }


        public override Response GetValue()
        {
            Response res = new Response();
            
            res.content = payload.valueVector.Sum().ToString();

            return res;
        }

        public override Response SetValue()
        {
            Response res = new Response();

            GCPayload pl = new GCPayload(uid, (int)Config.numReplicas, (int)Config.replicaId);

            pl.valueVector.Insert(pl.replicaid, this.parameters.GetParam<int>(0));

            this.payload = pl;
            
            return res;
        }

        public Response Increment()
        {
            Response res = new Response();


            this.payload.valueVector[this.payload.replicaid] += this.parameters.GetParam<int>(0);

            return res;

        }

    }


}