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

    public class Responses
    {

        public Status status;

        public List<string> contents { get; private set; }

        public List<Dest> destinations { get; private set; }

        private int contentLength = 0;

        public Responses(Status status)
        {
            contents = new List<string>();
            destinations = new List<Dest>();
            this.status = status;
        }

        public void AddReponse(Dest destination, string content = "", bool includeStatus = true)
        {

            string statusContent = content;

            if (includeStatus)
            {
                if (this.status == Status.fail)
                    statusContent = "Operation Failed\n" + statusContent;
                else
                    statusContent = "Operation Succeed\n" + statusContent;
            }

            this.contents.Add(statusContent);
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

}