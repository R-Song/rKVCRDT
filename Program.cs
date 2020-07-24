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

    public class Producer
    {

        // Demonstrates the production end of the producer and consumer pattern.
        public static void Produce(ITargetBlock<string> target)
        {
            // Create a Random object to generate random data.
            Random rand = new Random();

            // In a loop, fill a buffer with random data and
            // post the buffer to the target block.
            for (int i = 0; i < 100; i++)
            {
                // Create an array to hold random byte data.
                string str = "string " + i;
                Console.WriteLine("input: " + str);

                // Post the result to the message block.
                target.Post(str);
            }

            // Set the target to the completed state to signal to the consumer
            // that no more data will be available.
        }

        // Demonstrates the consumption end of the producer and consumer pattern.
        public static async Task ConsumeAsync(ISourceBlock<string> source)
        {
            // Initialize a counter to track the number of bytes that are processed.
            int bytesProcessed = 0;
            string data = null;

            // Read from the source buffer until the source buffer has no
            // available output data.

                while (await source.OutputAvailableAsync())
                {
                    data = source.Receive();
                    Console.WriteLine(data);
                }


        }

    }


    class Program
    {

        private const int portNum = 13;

        static void Main(string[] args)
        {
            Server ss = new Server();

            var handler = ss.HandleRequestAync();

            ss.Run();

            handler.Wait();

            //ss.Run();

            return;

            MessagePacket mp = new MessagePacket("127", "192", "a\nb\nc");

            string s = "a\nb\nc\nd";
            StringReader reader = new StringReader(s);
            reader.ReadLine();
            Console.WriteLine(reader.ReadToEnd());



            Console.WriteLine(new MessagePacket(mp.Serialize()));



            Config.numReplicas = 2;
            Config.replicaId = 0;

            API.initAPIs();


            string cmd1 =
@"gc
0
s
1
";

            string cmd2 =
@"gc
0
g
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

            Response res = Parser.RunCommand(cmd2);
            //Console.WriteLine(res.content);

            res = Parser.RunCommand(cmd3);
            //Console.WriteLine(res);

            res = Parser.RunCommand(cmd4);
            //Console.WriteLine(res);

            res = Parser.RunCommand(cmd2);
            Console.WriteLine(res);

            return;

            Console.WriteLine("Hello World!");
        }

    }
}
