using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

using RAC.Operations;
using static RAC.Errors.Log;

namespace RAC
{

    public class CRDTypeInfo
    {
        public Type type { get; }
        public Dictionary<string, MethodInfo> methodsList { get; set; }
        public Dictionary<string, List<string>> paramsList { get; set; }

        // used to check if 4 basic API exists
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

            foreach (string mp in methodParams)
            {
                if ((!API.converterList.ContainsKey(mp) && (!mp.Equals(""))))
                {
                    WARNING("Unable to load method: " + methodName +
                            " Param " + mp +
                            " does not exist in parameter/converter list");
                    return; 
                }
            }

            this.methodsList.Add(apiCode, m);
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

    static public partial class API
    {

        public static Dictionary<Type, CRDTypeInfo> typeList { set; get; }
        public static Dictionary<string, Type> typeCodeList { set; get; }

        public delegate object StringToType(string s);
        public delegate string TypeToString(object o);

        // First to type, then to string
        public static Dictionary<string, (StringToType, TypeToString)> converterList { set; get; }


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

        public static void InitAPIs()
        {
            typeList = new Dictionary<Type, CRDTypeInfo>();
            typeCodeList = new Dictionary<string, Type>();
            converterList = new Dictionary<string, (StringToType, TypeToString)>();

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

            LOG("The following CRDTs were added:\n" + PrintAllAPIs());
        }

        public static string PrintAllAPIs()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in typeList)
            {
                Type t = item.Key;
                CRDTypeInfo info = item.Value;

                sb.AppendLine("Type-" + t.ToString() + ":");

                foreach (var titem in info.methodsList)
                {
                    string pmstring = string.Join(",", info.paramsList[titem.Key]);
                    sb.AppendLine("API-" + titem.Value.ToString() + "<-" + pmstring);

                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

}