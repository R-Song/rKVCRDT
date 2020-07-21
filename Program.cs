using System;
using System.IO;
using RAC.Payloads;
using RAC.Operations;

using System.Collections.Generic;

namespace RAC
{

    class test
    {
        public int i {get; private set;}
        public test(int i)
        {
            this.i = i;
            
        }

        public override string ToString()
        {
            return "hahaha" + i;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            test t = new test(5);
            object o = (object)t;

            Console.WriteLine(t.i);


            return ;
            Config.numReplicas = 1;
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


            Parser.RunCommand(cmd1);

            Response res  =  Parser.RunCommand(cmd2);
            //Console.WriteLine(res.content);

            res  =  Parser.RunCommand(cmd3);
            //Console.WriteLine(res.content);

            res  =  Parser.RunCommand(cmd2);
            //Console.WriteLine(res.content);

            return; 

            /*
            GCounter g1 = new GCounter("1", pm1);
            g1.SetValue();
            g1.Save();

            GCounter g2 = new GCounter("1", pm1);
            Console.WriteLine(g2.GetValue().content);

             GCounter g3 = new GCounter("1", pm1);
             g3.Increment();
             g3.Save();

            GCounter g4 = new GCounter("1", pm1);
            Console.WriteLine(g4.GetValue().content);
            */

            Console.WriteLine("Hello World!");
        }
    }
}
