namespace RAC
{

    /// <summary>
    /// Please add new CRDT types, requests API methods
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
            // AddConverter("typename", Converters.Stringtotyoeconverter, Converters.Typetostringconverter)
            // IMPORTNAT: MUST ADD CONVERTERS FIRST
            // int
            AddConverter("int", Converters.StringToInt, Converters.IntToString);
            // string
            AddConverter("string", Converters.StringToStringO, Converters.StringOToString);
            // list of integers
            AddConverter("listi", Converters.StringToListi, Converters.ListiToString);
            // list of strings
            AddConverter("lists", Converters.StringToLists, Converters.ListsToString);

            //=========================================================================================//
            // Op History, DO NOT CHANGE THIS
            AddNewType("HistoryHandler", "h");
            // not used, just to keep parser happy
            AddNewAPI("HistoryHandler", "GetValue", "g", "");
            // not used, just to keep parser happy
            AddNewAPI("HistoryHandler", "SetValue", "s", "");
            AddNewAPI("HistoryHandler", "Synchronization", "y", "string, int");

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
            // Params: value - opid of related op, if "" then no relation
            AddNewAPI("RCounter", "Increment", "i", "int, string");
            AddNewAPI("RCounter", "Decrement", "d", "int, string");
            // Params: -opid to be reversed - string
            AddNewAPI("RCounter", "Reverse", "r", "string");
            // Params: nvector, pvector, new relation pair
            AddNewAPI("RCounter", "Synchronization", "y", "listi, listi, string");
            AddNewAPI("RCounter", "SynchronizeTombstone", "yr", "lists");

            // OR-Set
            AddNewType("ORSet", "os");
            AddNewAPI("ORSet", "GetValue", "g", "");
            AddNewAPI("ORSet", "SetValue", "s", "");
            AddNewAPI("ORSet", "Add", "a", "string");
            AddNewAPI("ORSet", "Remove", "rm", "string");
            AddNewAPI("ORSet", "Synchronization", "y", "lists, lists");

            
            // Reversible Graph
            AddNewType("RGraph", "rg");
            AddNewAPI("RGraph", "GetValue", "g", "");
            AddNewAPI("RGraph", "SetValue", "s", "");
            AddNewAPI("RGraph", "AddVertex", "av", "string");
            AddNewAPI("RGraph", "RemoveVertex", "rv", "string");
            AddNewAPI("RGraph", "AddEdge", "ae", "string, string");
            AddNewAPI("RGraph", "RemoveEdge", "re", "string, string");
            AddNewAPI("RGraph", "Synchronization", "y", "string, string");
            AddNewAPI("RGraph", "Reverse", "av", "string");
        }
    }
}