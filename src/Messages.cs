using System;
using System.Collections.Generic;
using System.Text;

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

        private int contentLength = 0;

        public Response(Status status)
        {
            contents = new List<string>();
            destinations = new List<Dest>();
            this.status = status;
        }

        public void AddContent(string content, Dest destination = Dest.none)
        {
            this.contents.Add(content);
            this.destinations.Add(destination);
            contentLength++;
        }

        public override string ToString() 
        {
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < this.contentLength; i++)
            {   
                sb.AppendFormat("Content: \n -------- \n{0} \n -------- \n is on its way to {1} ", contents[i], destinations[i]);
            }

            return sb.ToString();
        }


    }

    public class Request
    {
        public string content;
    }

}