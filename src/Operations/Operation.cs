

namespace RAC.Operations
{
    public interface IOperation
    {
        Response SetValue(Object obj, Request req);
        Response GetValue(Object obj);
        Response UpdateValue(Object obj, Request req);
        Response Synchronization(Object obj);
        Response Delete(Object obj);
        
    }

}