using System;

namespace RAC.Errors
{
    public class PayloadNotFoundException : Exception
    {
        public PayloadNotFoundException()
        {
        }

        public PayloadNotFoundException(string message)
        : base(message)
        {
        }
    }
}