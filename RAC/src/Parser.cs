using System; 
using System.IO; 
using System.Collections.Generic;
using System.Text;
using RAC.Errors;
using RAC.Network;

using static RAC.Errors.Log;

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

        public static bool ParseCommand(MsgSrc source, string cmd, out string typeCode, out string apiCode, out string uid, out Parameters pm, out Clock clock)
        {

            List<string> parameters = new List<string>();

            typeCode = "";
            apiCode = "";
            uid = "";
            pm = null;
            clock = null;

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
                        case 3:
                            if (source == MsgSrc.server)
                            {
                                try
                                {
                                    clock = Clock.FromString(line);
                                }
                                catch (InvalidMessageFormatException)
                                {
                                    return false;
                                }
                            } 
                            else
                            {
                                clock = null;
                                parameters.Add(line);     
                            }
                        
                            break;
                        default:
                            parameters.Add(line);
                            break;

                    }
                    lineNumeber++;
                } 

                if (lineNumeber < 2)
                {
                    WARNING("Incorrect command format: " + cmd);
                    return false;
                }
            } 

            pm = ParamBuilder(typeCode, apiCode, parameters);
            return true;
        }

        public static Responses RunCommand(string cmd, MsgSrc source)
        {
            string typeCode;
            string uid;
            string apiCode;
            Parameters pm;
            Clock clock;
            Responses res;

            if (!ParseCommand(source, cmd, out typeCode, out apiCode, out uid, out pm, out clock))
            {
                res = new Responses(Status.fail);
                res.AddReponse(Dest.client, "Incorrect command format " + cmd);
                return res;
            }

            res = API.Invoke(typeCode, uid, apiCode, pm, clock);
            return res;
        }

        public static string BuildCommand(string typeCode, string apiCode, string uid, Parameters pm, Clock clock = null)
        {
            StringBuilder sb = new StringBuilder(64);
            sb.AppendLine(typeCode);
            sb.AppendLine(uid);
            sb.AppendLine(apiCode);
            if (clock is null)
                sb.AppendLine("0:0:0");
            else
                sb.AppendLine(clock.ToString());


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