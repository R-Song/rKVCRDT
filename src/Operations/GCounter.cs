using RAC.Payloads;
using System.Linq;
using System.Collections;

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

            GCPayload pl = new GCPayload(uid, Config.numReplicas);

            pl.valueVector.Insert(0, this.parameters.GetParam<int>(0));

            this.payload = pl;
            
            return res;
        }
    }


}