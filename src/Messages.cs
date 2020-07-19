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


    }

    public class Request
    {
        public string content;
    }

}