
using System.Collections.Generic;
using System.Linq;

namespace RAC
{

    /// <summary>
    /// Please add new CRDT types, requests API methods, type converters in this class.
    /// </summary>
    static public partial class API
    {       
            /// <summary>
            /// Add Converters, Replicated Types and APIs here.
            /// See:
            /// <see cref="API.AddNewType"/> 
            /// <see cref="API.AddNewAPI(string, string, string, string)"/>
            /// <see cref="API.AddConverter"/>  
            ///  </summary>
            private static void APIs()
            {
                // Add data types that will be used below:
                // IMPORTNAT: MUST ADD CONVERTERS FIRST
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
                AddNewAPI("GCounter", "Synchronization", "y", "listi");
                AddNewAPI("GCounter", "Increment", "i", "int");
                
                // Reversible Counter
                AddNewType("RCounter", "rc");
                AddNewAPI("RCounter", "GetValue", "g", "");
                AddNewAPI("RCounter", "SetValue", "s", "int");
                AddNewAPI("RCounter", "Synchronization", "y", "listi, listi");
                AddNewAPI("RCounter", "Increment", "i", "int");
                AddNewAPI("RCounter", "Decrement", "d", "int");
                // Params: op_id to be reversed - string
                AddNewAPI("RCounter", "Reverse", "r", "string");
                

                
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