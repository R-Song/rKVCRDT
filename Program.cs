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

            Response res  =  Parser.RunCommand(cmd2);
            //Console.WriteLine(res.content);

            res  =  Parser.RunCommand(cmd3);
            //Console.WriteLine(res);

            res  =  Parser.RunCommand(cmd4);
            //Console.WriteLine(res);

            res  =  Parser.RunCommand(cmd2);
            Console.WriteLine(res);

            return; 

            Console.WriteLine("Hello World!");
        }
    }
}
