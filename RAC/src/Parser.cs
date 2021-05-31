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
        private const string typePrefix = "TYPE:";
        private const string uidPrefix = "UID:";
        private const string opPrefix = "OP:";
        private const string clockPrefix = "CLK:";
        private const string paramPrefix = "P:";

        private static Parameters ParamBuilder(string typeCode, string apiCode, List<string> input)
        {
            List<string> pmTypesConverters = API.typeList[API.typeCodeList[typeCode]].paramsList[apiCode];
            Parameters pm = new Parameters(pmTypesConverters.Count);

            for (int i = 0; i < input.Count; i++)
            {
                object data;

                try
                {
                    API.StringToType toType = API.GetToTypeConverter(pmTypesConverters[i]);
                    data = toType(input[i]);
                    pm.AddParam(i, data);
                } 
                catch (Exception e)
                {
                    ERROR("Param building failed with: " + apiCode + " of " + typeCode, e);
                }
            }

            return pm;
        }

        public static bool ParseCommand(MsgSrc source, string cmd, out string typeCode, out string apiCode, out string uid, out Parameters pm)
        {

            List<string> parameters = new List<string>();

            typeCode = "";
            apiCode = "";
            uid = "";
            pm = null;
            Clock clock = null; // TODO: remove this

            using (StringReader reader = new StringReader(cmd)) 
            { 
                string line;
                bool onParam = false; 
                string paramstr = "";

                while ((line = reader.ReadLine()) != null) 
                { 
                    if (line.StartsWith(typePrefix, StringComparison.Ordinal))
                        typeCode = line.Remove(0, typePrefix.Length).Trim('\n',' ');
                    else if (line.StartsWith(uidPrefix, StringComparison.Ordinal))
                        uid = line.Remove(0, uidPrefix.Length).Trim('\n',' ');
                    else if (line.StartsWith(opPrefix, StringComparison.Ordinal))
                        apiCode = line.Remove(0, opPrefix.Length).Trim('\n',' ');
                    else if (line.StartsWith(clockPrefix, StringComparison.Ordinal))
                    {
                        string t = line.Remove(0, clockPrefix.Length).Trim('\n',' ');
                        if (source == MsgSrc.server)
                        {
                            try
                            {
                                clock = Clock.FromString(t);
                            }
                            catch (InvalidMessageFormatException)
                            {
                                return false;
                            }
                        } 
                        
                    } 
                    else if (line.StartsWith(paramPrefix, StringComparison.Ordinal))
                    {
                        // if not the first P: seen
                        if (onParam)
                            parameters.Add(paramstr);

                        // first line of param str
                        paramstr = line.Remove(0, paramPrefix.Length);   
                        onParam = true;
                    }
                    else
                    {
                        // in paramstring block
                        if (onParam)
                            paramstr += line;
                    }
                }
                // last param
                if (onParam)
                    parameters.Add(paramstr);
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
            Responses res;

            if (!ParseCommand(source, cmd, out typeCode, out apiCode, out uid, out pm))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "Incorrect command format " + cmd);
                return res;
            }


            res = API.Invoke(typeCode, uid, apiCode, pm);
            
            // stats
            if (source == MsgSrc.client)
            {
                Profiler.clientOpsTotal += 1;
                if (res.status == Status.success)
                    Profiler.clientOpsSuccess += 1;
            }

            return res;
        }

        public static string BuildCommand(string typeCode, string apiCode, string uid, Parameters pm, Clock clock = null)
        {
            StringBuilder sb = new StringBuilder(64);
            sb.AppendLine(typePrefix + typeCode);
            sb.AppendLine(uidPrefix + uid);
            sb.AppendLine(opPrefix + apiCode);
            if (clock is null)
                sb.AppendLine(clockPrefix + "0:0:0");
            else
                sb.AppendLine(clockPrefix + clock.ToString());


            API.TypeToString toStr = null;
            List<string> pmTypesConverters = API.typeList[API.typeCodeList[typeCode]].paramsList[apiCode];
            
            for (int i = 0; i < pm.size; i++)
            {
                object o = pm.AllParams()[i];
                toStr = API.GetToStringConverter(pmTypesConverters[i]);
                sb.Append(paramPrefix + toStr(o) + "\n");
            }
            return sb.ToString();

        }


    }

}