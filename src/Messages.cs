namespace RAC
{

    public enum Status 
    {
        success,
        fail

    }

    public class Response
    {


        public string content;

        public Status status;
    }

    public class Request
    {
        public string content;
    }

}