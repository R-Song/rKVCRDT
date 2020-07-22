using System; 
using System.IO; 
using System.Collections.Generic;
using System.Text;

namespace RAC
{   
    public static partial class Parser
    {   


        private static Parameters ParamBuilder(string typeCode, string apiCode, List<string> input)
        {
            List<string> pmTypesConverters = API.typeList[API.typeCodeList[typeCode]].paramsList[apiCode];
            Parameters pm = new Parameters(pmTypesConverters.Count);

            for (int i = 0; i < input.Count; i++)
            {
                object data;

                API.StringToType toType = API.GetToTypeConverter(pmTypesConverters[i]);
               
                data = toType(input[i]);
                pm.AddParam(i, data);
                
            }

            return pm;
        }

        public static bool ParseCommand(string cmd, out string typeCode, out string apiCode, out string uid, out Parameters pm)
        {

            List<string> parameters = new List<string>();

            typeCode = "";
            apiCode = "";
            uid = "";
            pm = null;

            using (StringReader reader = new StringReader(cmd)) 
            { 
                int lineNumeber = 0;
                string line; 
                while ((line = reader.ReadLine()) != null) 
                { 
                    switch(lineNumeber)
                    {
                        case 0: 
                            typeCode = line;
                            break;
                        case 1:
                            uid = line;
                            break;
                        case 2:
                            apiCode = line;
                            break;
                        default:
                            parameters.Add(line);
                            break;

                    }
                    lineNumeber++;
                } 

                if (lineNumeber < 2)
                {
                    //TODO: send error messages
                    return false;
                }
            } 

            pm = ParamBuilder(typeCode, apiCode, parameters);
            return true;
        }

        public static Response RunCommand(string cmd)
        {

            string typeCode;
            string uid;
            string apiCode;
            Parameters pm;
            Response res;

            if (!ParseCommand(cmd, out typeCode, out apiCode, out uid, out pm))
            {
                // TODO: send error
                return new Response(Status.fail);
            }

            res = API.Invoke(typeCode, uid, apiCode, pm);

            return res;
        }

        public static string BuildCommand(string typeCode, string apiCode, string uid, Parameters pm)
        {
            StringBuilder sb = new StringBuilder(64);
            sb.AppendLine(typeCode);
            sb.AppendLine(uid);
            sb.AppendLine(apiCode);

            API.TypeToString toStr = null;
            List<string> pmTypesConverters = API.typeList[API.typeCodeList[typeCode]].paramsList[apiCode];
            
            for (int i = 0; i < pm.size; i++)
            {
                object o = pm.AllParams()[i];
                toStr = API.GetToStringConverter(pmTypesConverters[i]);
                sb.Append(toStr(o));
            }
            return sb.ToString();

        }


    }

}