using System;
using RAC.Payloads;
using RAC.Operations;

namespace RAC
{
    class Program
    {
        static void Main(string[] args)
        {
            Parameters pm1 = new Parameters(1);
            pm1.addParam<int>(0, 1);

            GCounter g1 = new GCounter("1", pm1);
            g1.SetValue();
            g1.Save();

            GCounter g2 = new GCounter("1", pm1);

            Console.WriteLine(g2.GetValue().content);


            Console.WriteLine("Hello World!");
        }
    }
}
