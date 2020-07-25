using System;
using System.Diagnostics;
using System.IO;

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

    public static class Log
    {
        public static bool errToLogFile = false;
        public static bool warningToLogFile = false;
        public static bool logToLogFile = false;
        private static StreamWriter file = null;
        private static TextWriter errorWriter = Console.Error;

        public static void LogInit()
        {

        }


        public static string Curtime() 
        {
            return DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss");
        }

        [Conditional("DEBUG")]
        public static void DEBUG(string str)
        {
            Console.WriteLine("-DEBUG- {0}:\n {1} \n", Curtime(), str);
        }

        public static void LOG(string str)
        {
            string s = String.Format("-LOG- {0}:\n {1} \n", Curtime(), str);

            if (logToLogFile)
                file.Write(s);
            else
                Console.Write(s);
            
        }

        public static void WARNING(string str)
        {
            string s = String.Format("-⚠️WARNING⚠️- {0}:\n {1} \n", Curtime(), str);

            if (logToLogFile)
                file.Write(s);
            else
                Console.Write(s);
        }

        public static void ERROR(string str)
        {
            string s = String.Format("-🚨ERROR🚨- {0}:\n {1} \n", Curtime(), str);

            if (logToLogFile)
                file.Write(s);
            else
                errorWriter.Write(s);
            
        }
        
    }
}