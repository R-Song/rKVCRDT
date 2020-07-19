using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

using RAC.Operations;

namespace RAC
{
    public class CRDType
    {
        public Type type;
        public Dictionary<string, MethodInfo> MethodsList;
        public Dictionary<string, string[]> ParamsList;

        public CRDType(Type type)
        {
            this.type = type;
            MethodsList = new Dictionary<string, MethodInfo>();
            ParamsList = new Dictionary<string, string[]>();
        }

        public void AddNewAPI(string apiCode, string methodName, string[] methodParams)
        {
            MethodInfo m = this.type.GetMethod(methodName);
            this.MethodsList.Add(apiCode, m);
            this.ParamsList.Add(apiCode, methodParams);
        }

        public Parameters StringListToParams(string[] methodParams)
        {
            Parameters parameters = new Parameters(methodParams.Length);

            foreach (string p in methodParams)
            {

            }

            return parameters;

        }


    }

    static partial class API
    {

        public delegate void APIDel();

        public static Dictionary<Type, CRDType> typeList = new Dictionary<Type, CRDType>();
        public static Dictionary<string, Type> typeCodeList = new Dictionary<string, Type>();

        public static void AddNewType(string typeName, string typeCode)
        {
            // TODO: sanity check
            Type t = Type.GetType(typeName);

            typeCodeList.Add(typeCode, t);
            typeList.Add(t, new CRDType(t));
        }

        
        public static void AddNewAPI(string typeName, string methodName, string apiCode, string methodParams)
        {
            // TODO: sanity check
            Type t = Type.GetType(typeName);

            CRDType type = typeList[t];
            type.AddNewAPI(apiCode, methodName, methodParams.Split(','));

        }

        public static void APIs()
        {
            // GCounter
            AddNewType("GCounter", "gc");
            AddNewAPI("GCounter", "GetValue", "g", "");
            AddNewAPI("GCounter", "SetValue", "s", "int");
            

        }



    }

}