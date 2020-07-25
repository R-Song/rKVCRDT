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

namespace RAC
{



    class Program
    {

        private const int portNum = 13;

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("need 1 arg");
                return 1;
            }

            int self = Int32.Parse(args[0]);


            Node n1 = new Node(0, "127.0.0.1", 5000);
            Node n2 = new Node(0, "127.0.0.1", 5001);

            Global.cluster.Add(n1);
            Global.cluster.Add(n2);
            
            Global.cluster[self].isSelf = true;
            Global.selfNode = Global.cluster[self];
            Config.numReplicas = 2;

            API.initAPIs();

            Server ss = new Server();

            var asynchanlder = ss.HandleRequestAync();
            var asynchanlder2 = ss.SendResponseAsync();

            ss.Run();

            //handler.Wait();

            return 0;

            Thread.Sleep(1000);
            string cmd2 =
@"gc
0
g
";

            Response res = Parser.RunCommand(cmd2);
            Console.WriteLine(res);


            return 0;

            MessagePacket mp = new MessagePacket("127", "192", "a\nb\nc");

            string s = "a\nb\nc\nd";
            StringReader reader = new StringReader(s);
            reader.ReadLine();
            Console.WriteLine(reader.ReadToEnd());

            Config.numReplicas = 2;
            Config.replicaId = 0;




            string cmd1 =
@"gc
0
s
1
";



            string cmd3 =
@"gc
0
i
5
";

            string cmd4 =
@"gc
0
y
0,2,3
";



            Parser.RunCommand(cmd1);

            res = Parser.RunCommand(cmd2);
            //Console.WriteLine(res.content);

            res = Parser.RunCommand(cmd3);
            //Console.WriteLine(res);

            res = Parser.RunCommand(cmd4);
            //Console.WriteLine(res);



            return 0;

            Console.WriteLine("Hello World!");
        }

    }
}
