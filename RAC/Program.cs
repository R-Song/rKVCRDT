using System;
using RAC.Network;



namespace RAC
{
    class Program
    {
        // use proper versioning
        static string VERSION = "2";

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please provide correct json cluster config file");
                return 1;
            }

            Console.WriteLine("Running rac version " + VERSION);

            string nodeconfigfile = args[0];

            Global.init(nodeconfigfile);
            Global.server.Run();



            return 0;

        }

    }
}
