using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

using RAC.Operations;

namespace RAC
{

    public class CRDType
    {
        public Type type;
        // TODO: setters ang getters...
        public Dictionary<string, MethodInfo> methodsList;
        public Dictionary<string, List<Type>> paramsList;

        public CRDType(Type type)
        {
            this.type = type;
            methodsList = new Dictionary<string, MethodInfo>();
            paramsList = new Dictionary<string, List<Type>>();
        }

        public void AddNewAPI(string apiCode, string methodName, string[] methodParams)
        {
            MethodInfo m = this.type.GetMethod(methodName);
            this.methodsList.Add(apiCode, m);
            
            List<Type> tlist = new List<Type>();

            if (methodParams.Length > 0)
            {
                foreach (string p in methodParams)
                {
                    Type tt = null;

                    string lowerp = p.ToLower();
                    switch (lowerp)
                    {
                        case "":
                            continue;
                        case "int":
                        case "int32":
                        case "integer":
                            tt = typeof(System.Int32);
                            break;
                        case "float":
                            tt = typeof(System.Single);
                            break;
                        case "string":
                            tt = typeof(System.String);
                            break;
                        default:
                            try {
                                tt = Type.GetType(p, true);
                            }
                            catch(TypeLoadException e) {
                                // TODO: print error
                                return;
                            }
                            break;

                    }
                    
                    tlist.Add(tt);
                }
            }

            this.paramsList.Add(apiCode, tlist);
            
        }



    }

    static partial class API
    {

        public static Dictionary<Type, CRDType> typeList = new Dictionary<Type, CRDType>();
        public static Dictionary<string, Type> typeCodeList = new Dictionary<string, Type>();

        

        public static void AddNewType(string typeName, string typeCode)
        {
            // TODO: sanity check
            Type t = Type.GetType("RAC.Operations." + typeName);

            typeCodeList.Add(typeCode, t);
            typeList.Add(t, new CRDType(t));
        }
        
        public static void AddNewAPI(string typeName, string methodName, string apiCode, string methodParams)
        {
            // TODO: sanity check
            Type t = Type.GetType("RAC.Operations." + typeName);

            CRDType type = typeList[t];
            type.AddNewAPI(apiCode, methodName, methodParams.Split(','));

        }

        public static Response Invoke(string typeCode, string uid, string apiCode, Parameters parameters)
        {
            Type opType = typeCodeList[typeCode];
            CRDType t = typeList[opType];

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
        }

        private static void APIs() // TODO: move this to another file
        {
            // TODO: add a type conversion table

            // GCounter
            AddNewType("GCounter", "gc");
            AddNewAPI("GCounter", "GetValue", "g", "");
            AddNewAPI("GCounter", "SetValue", "s", "int");
            AddNewAPI("GCounter", "Increment", "i", "int");
            

        }



    }



}