using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

using RAC.Operations;
using static RAC.Errors.Log;

namespace RAC
{

    public class CRDTypeInfo
    {
        public Type type;
        public Dictionary<string, MethodInfo> methodsList;
        public Dictionary<string, List<string>> paramsList;

        // if 4 basic API exists
        private List<string> checklist = new List<string>();

        public CRDTypeInfo(Type type)
        {
            this.type = type;
            methodsList = new Dictionary<string, MethodInfo>();
            paramsList = new Dictionary<string, List<string>>();
        }

        public void AddNewAPI(string apiCode, string methodName, string[] methodParams)
        {
            MethodInfo m = this.type.GetMethod(methodName);

            if (m is null)
            {
                WARNING("Unable to load method: " + methodName);
                return;
            }

            this.methodsList.Add(apiCode, m);
            // TODO: check params type
            this.paramsList.Add(apiCode, new List<string>(methodParams));
            
            checklist.Add(methodName);
        }

        public bool CheckBasicAPI(out string missing)
        {
            bool flag = true;
            missing = "";

            if (!checklist.Contains("GetValue"))
            {
                missing += "GetValue ";
                flag = false;
            }

            if (!checklist.Contains("SetValue"))
            {
                missing += "SetValue ";
                flag = false;
            }

            if (!checklist.Contains("Synchronization"))
            {
                missing += "Synchronization ";
                flag = false;
            }

            return flag;

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
        public delegate Responses CRDTOPMethod();

        public static void AddNewType(string typeName, string typeCode)
        {
            Type t;
            try
            {
                t = Type.GetType("RAC.Operations." + typeName, true);
                typeCodeList.Add(typeCode, t);
                typeList.Add(t, new CRDTypeInfo(t));
            }
            catch (TypeLoadException)
            {
                WARNING("Unable to load CRDT: " + typeName);
                
            }

        }
        
        public static void AddNewAPI(string typeName, string methodName, string apiCode, string methodParams)
        {            
            Type t;

            try
            {
                t = Type.GetType("RAC.Operations." + typeName, true);
            }
            catch (TypeLoadException)
            {
                WARNING("Unable to load CRDT: " + typeName + ", skip adding " + methodName);
                return;
            }

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


        public static Responses Invoke(string typeCode, string uid, string apiCode, Parameters parameters)
        {
            Type opType = typeCodeList[typeCode];
            CRDTypeInfo t = typeList[opType];

            MethodInfo method = t.methodsList[apiCode];

            var opObject = Convert.ChangeType(Activator.CreateInstance(opType, new object[]{uid, parameters}), opType);
            Responses res = (Responses)method.Invoke(opObject, null);
            
            MethodInfo saveMethod = opObject.GetType().GetMethod("Save");
            saveMethod.Invoke(opObject, null);

            return res;
        }

        public static void initAPIs()
        {
            APIs();

            // check if all types has get, set, sync, delete after finish loading API
            foreach (KeyValuePair<Type, CRDTypeInfo> entry in typeList)
            {
                string msg;
                if (!entry.Value.CheckBasicAPI(out msg))
                {
                    WARNING(String.Format("Following basic APIs for type {0} not found, removing the type: {1}", entry.Key.ToString(), msg));
                    typeList.Remove(entry.Key);
                }
            }
        }
    }

}