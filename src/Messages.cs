using System.Collections.Generic;

namespace RAC
{

    public enum Status
    {
        success,
        fail

    }

    public enum Dest
    {
        client,
        broadcast,
        none

    }

    public class Response
    {

        public Status status;

        public List<string> contents { get; private set; }

        public List<Dest> destinations { get; private set; }

        public Response(Status status)
        {
            this.status = status;
        }

        public void AddContent(string content, Dest destination = Dest.none)
        {
            this.contents.Add(content);
            this.destinations.Add(destination);
        }



    }

    public class Request
    {
        public string content;
    }

}