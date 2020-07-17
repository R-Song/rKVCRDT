
using RAC.Payloads;
using System.Linq;

namespace RAC.Operations
{
    public class GCounterOp : IOperation
    {
        Response IOperation.Delete(Object obj)
        {
            throw new System.NotImplementedException();
        }

        Response IOperation.GetValue(Object obj)
        {
            Response res = new Response();

            GCPayload pl = Global.Gcountervec[obj.uid];

            res.content = pl.valueVector.Sum().ToString();
            res.status = Status.success;
            return res;

        }

        Response IOperation.SetValue(Object obj, Request req)
        {
            int val = int.Parse(req.content);

            GCPayload pl = new GCPayload(obj.uid, Config.numReplicas);
            pl.valueVector[(int)Config.replicaId] = val;

            Global.Gcountervec[obj.uid] = pl;

            Response res = new Response();
            res.status = Status.success;
            return res;
        }

        Response IOperation.Synchronization(Object obj)
        {
            throw new System.NotImplementedException();
        }

        Response IOperation.UpdateValue(Object obj, Request req)
        {
            throw new System.NotImplementedException();
        }
    }

}