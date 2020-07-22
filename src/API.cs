using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

using RAC.Operations;

namespace RAC
{

    public class CRDTypeInfo
    {
        public Type type;
        // TODO: setters ang getters...
        public Dictionary<string, MethodInfo> methodsList;
        public Dictionary<string, List<string>> paramsList;

        public CRDTypeInfo(Type type)
        {
            this.type = type;
            methodsList = new Dictionary<string, MethodInfo>();
            paramsList = new Dictionary<string, List<string>>();
        }

        public void AddNewAPI(string apiCode, string methodName, string[] methodParams)
        {
            MethodInfo m = this.type.GetMethod(methodName);
            this.methodsList.Add(apiCode, m);
            this.paramsList.Add(apiCode, new List<string>(methodParams));
        }



    }

    static partial class API
    {

        public static Dictionary<Type, CRDTypeInfo> typeList = new Dictionary<Type, CRDTypeInfo>();
        public static Dictionary<string, Type> typeCodeList = new Dictionary<string, Type>();

        public delegate object StringToType(string s);
        public delegate string TypeToString(object o);

        // First to type, then to string
        public static Dictionary<string, (StringToType, TypeToString)> converterList = new Dictionary<string, (StringToType, TypeToString)>();


        // TODO: MAYBE, use delegate here
        public delegate Response CRDTOPMethod();

        public static void AddNewType(string typeName, string typeCode)
        {
            // TODO: sanity check
            Type t = Type.GetType("RAC.Operations." + typeName);

            typeCodeList.Add(typeCode, t);
            typeList.Add(t, new CRDTypeInfo(t));
        }
        
        public static void AddNewAPI(string typeName, string methodName, string apiCode, string methodParams)
        {
            // TODO: sanity check
            Type t = Type.GetType("RAC.Operations." + typeName);

            CRDTypeInfo type = typeList[t];
            type.AddNewAPI(apiCode, methodName, methodParams.Split(','));

        }

        public static void AddConverter(string paramType, StringToType ToType, TypeToString ToString)
        {
            converterList.Add(paramType, (ToType, ToString));
        }

        public static StringToType GetToTypeConverter(string paramType)
        {
            return converterList[paramType].Item1;
        }

        public static TypeToString GetToStringConverter(string paramType)
        {
            return converterList[paramType].Item2;
        }


        public static Response Invoke(string typeCode, string uid, string apiCode, Parameters parameters)
        {
            Type opType = typeCodeList[typeCode];
            CRDTypeInfo t = typeList[opType];

            MethodInfo method = t.methodsList[apiCode];

            var opObject = Convert.ChangeType(Activator.CreateInstance(opType, new object[]{uid, parameters}), opType);
            Response res = (Response)method.Invoke(opObject, null);
            
            MethodInfo saveMethod = opObject.GetType().GetMethod("Save");
            saveMethod.Invoke(opObject, null);

            return res;
        }

        public static void initAPIs()
        {
            APIs();
            //TODO: check if all types has get, set, sync, delete after finish loading API
        }

        private static void APIs() // TODO: move this to another file
        {
            // int
            AddConverter("int", Converters.StringToInt, Converters.IntToString);
            // string
            AddConverter("string", Converters.StringToStringO, Converters.StringOToString);
            // list of integers
            AddConverter("listi", Converters.StringToListi, Converters.ListiToString);


            // GCounter
            AddNewType("GCounter", "gc");
            AddNewAPI("GCounter", "GetValue", "g", "");
            AddNewAPI("GCounter", "SetValue", "s", "int");
            AddNewAPI("GCounter", "Increment", "i", "int");
            AddNewAPI("GCounter", "Synchronization", "y", "listi");
            
            

        }

        public static class Converters
        {
            public static string ListiToString(object l)
            {   
                List<int> lst = (List<int>)l;
                return string.Join(",", lst);
            }

            public static List<int> StringToListi(string s)
            {   
                return s.Split(",").Select(int.Parse).ToList();
            }

            public static string IntToString(object i)
            {
                return ((int)i).ToString();
            }

            public static object StringToInt(string s)
            {
                return int.Parse(s);
            }

            public static string StringOToString(object i)
            {
                return (string)i;
            }

            public static object StringToStringO(string i)
            {
                return (object)i;
            }



        }



    }



}