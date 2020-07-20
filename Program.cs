using System;
using RAC.Payloads;
using RAC.Operations;

namespace RAC
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.numReplicas = 1;
            Config.replicaId = 0;

            API.initAPIs();

            Parameters pm1 = new Parameters(1);
            pm1.addParam<int>(0, 1);

            API.Invoke("gc", "0", "s", pm1);

            Response res  =  API.Invoke("gc", "0", "g", pm1);
            Console.WriteLine(res.content);

            API.Invoke("gc", "0", "i", pm1);

            res  =  API.Invoke("gc", "0", "g", pm1);
            Console.WriteLine(res.content);
            
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
