﻿using System;
using RAC.Network;

namespace RAC
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please provide correct json cluster config file");
                return 1;
            }

            string nodeconfigfile = args[0];

            Global.init(nodeconfigfile);

            var recieveHandler = Global.server.HandleRequestAsync();
            var sendHandler = Global.server.SendResponseAsync();

            Global.server.Run();



            return 0;

        }

    }
}
