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

        // set this to its typecode
        public override string typecode { get ; set; } = "";

        public Template(string uid, Parameters parameters) : base(uid, parameters)
        {
            // put any necessary metadata here
        }


        public override Responses GetValue()
        {
            // to set up responses
            Responses res;
            res = new Responses(Status.success);
            
            // to add client response
            res.AddResponse(Dest.client, "response string here"); 

            // to make a broadcast for synchronization
            res.AddResponse(Dest.broadcast, "broad cast string here", false);
            
            throw new NotImplementedException();

            // return response at the end
            return res;

        }

        public override Responses SetValue()
        {
            // init payload here
            TemplatePayload pl = new TemplatePayload(uid);

            // to get a request parameter
            // this.parameters.GetParam<type>(index);
            
            // save payload
            this.payload = pl;

            throw new NotImplementedException();
        }

        public override Responses Synchronization()
        {
            throw new NotImplementedException();
        }
    }



}

