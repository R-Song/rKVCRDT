using System;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Useful things:
    /// Payload: this.payload
    /// Parameters: this.parameters
    ///     get a parameter: this.parameters.GetParam<T>(int index)
    ///     create new Parameters for broadcast: Parameters syncPm = new Parameters(int numparams);
    ///     add values to Paramters: syncPm.AddParam(int index, object value)
    ///     Create parameter string: Parser.BuildCommand(string typeCode, string apiCode, string uid, Parameters pm)
    /// Create new Response: Responses res = new Responses(Status status)
    ///     Add content to Response res.AddResponse(Dest dest, string content = "", bool includeStatus = true)
    /// Access op history: this.history
    /// </summary>
    public class Template : Operation<TemplatePayload>
    {

        // todo: set this to its typecode
        public override string typecode { get ; set; } = "";

        public Template(string uid, Parameters parameters) : base(uid, parameters)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            throw new NotImplementedException();
        }

        public override Responses SetValue()
        {
            throw new NotImplementedException();
        }

        public override Responses Synchronization()
        {
            throw new NotImplementedException();
        }
    }



}

