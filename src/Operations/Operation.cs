
namespace RAC.Operations
{
    public interface IOperation
    {
        Response SetValue(string uid, Request req);
        Response GetValue(string uid);
        Response UpdateValue(string uid, Request req);
        Response Synchronization(string uid);
        Response Delete(string uid);
        
    }

}