using System;
using RAC.Network;

namespace RAC
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("need 1 arg");
                return 1;
            }

            string nodeconfigfile = args[0];

            Global.init(nodeconfigfile);

            Server ss = new Server(Global.selfNode);

            var asynchanlder = ss.HandleRequestAync();
            var asynchanlder2 = ss.SendResponseAsync();

            ss.Run();

            //handler.Wait();

            return 0;

        }

    }
}
