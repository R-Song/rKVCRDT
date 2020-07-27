using System;
using System.IO;
using RAC.Payloads;
using RAC.Operations;
using RAC.Network;

using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Threading;

using Newtonsoft.Json;

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

            Server ss = new Server();

            var asynchanlder = ss.HandleRequestAync();
            var asynchanlder2 = ss.SendResponseAsync();

            ss.Run();

            //handler.Wait();

            return 0;

        }

    }
}
