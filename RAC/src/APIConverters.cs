using System.Collections.Generic;
using System.Linq;

namespace RAC
{

    /// <summary>
    /// String to Type converters used by API
    /// </summary>
    static public partial class API
    {
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

            // string list
            public static string ListsToString(object l)
            {
                List<string> lst = (List<string>)l;
                return string.Join(",", lst);
            }

            public static List<string> StringToLists(string s)
            {
                return s.Split(",").Select(p => p.Trim()).ToList();
            }

            // add type converter API below

        }

    }
}