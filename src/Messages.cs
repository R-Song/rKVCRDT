namespace RAC
{

    public enum Status 
    {
        success,
        fail

    }

    public class Response
    {


        public string content; // TODO: add setter that covert everything to string

        public Status status;

        public bool ifBroadcastUpdate;
        public string broadcast;


    }

    public class Request
    {
        public string content;
    }

}