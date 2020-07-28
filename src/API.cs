
using System.Collections.Generic;
using System.Linq;

namespace RAC
{

    static partial class API
    {
            private static void APIs()
            {
                // Add data types that will be used below:
                // int
                AddConverter("int", Converters.StringToInt, Converters.IntToString);
                // string
                AddConverter("string", Converters.StringToStringO, Converters.StringOToString);
                // list of integers
                AddConverter("listi", Converters.StringToListi, Converters.ListiToString);

                //=========================================================================================//

                // ADD CRDTs and their APIs below:
                // GCounter
                AddNewType("GCounter", "gc");
                AddNewAPI("GCounter", "GetValue", "g", "");
                AddNewAPI("GCounter", "SetValue", "s", "int");
                AddNewAPI("GCounter", "Increment", "i", "int");
                AddNewAPI("GCounter", "Synchronization", "y", "listi");

                
            }


        public static class Converters
        {
            // Integer list
            public static string ListiToString(object l)
            {   
                List<int> lst = (List<int>)l;
                return string.Join(",", lst);
            }

            public static List<int> StringToListi(string s)
            {   
                return s.Split(",").Select(int.Parse).ToList();
            }

            // int
            public static string IntToString(object i)
            {
                return ((int)i).ToString();
            }

            public static object StringToInt(string s)
            {
                return int.Parse(s);
            }

            // string
            public static string StringOToString(object i)
            {
                return (string)i;
            }

            public static object StringToStringO(string i)
            {
                return (object)i;
            }

            // add type converter API below

        }
    }
}