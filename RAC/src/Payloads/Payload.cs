namespace RAC.Payloads
{
    /// <summary>
    /// This is the base class that holds information on storaging the states
    /// of a CRDT object. You should extend this class the add necessary
    /// data structure for a new CRDT.
    /// Example:
    /// public class GCPayload : Payload
    /// {
    /// ...
    ///    // This is the vector of values that represent a G-Counter
    ///    public List<int> valueVector {set; get;} 
    /// ...
    /// }
    ///  
    /// </summary>
    public abstract class Payload
    {
        public string uid { get; protected set; }
        public long size { get; protected set; }

    }

}
