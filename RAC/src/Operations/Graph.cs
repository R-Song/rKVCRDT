using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Reversible Graph
    /// </summary>
    public class Graph : Operation<RGraphPayload>
    {
        private const int TAG_LEN = 8;

        // todo: set this to its typecode
        public override string typecode { get; set; } = "gh";

        public Graph(string uid, Parameters parameters) : base(uid, parameters)
        {
            // todo: put any necessary data here
        }
        public override Responses GetValue()
        {

            Responses res = new Responses(Status.success);



            var addedVertices = new HashSet<(string value, string tag)>();
            var removedVertices = new HashSet<(string value, string tag)>();
            var addedEdges = new HashSet<((string v1, string v2), string tag)>();
            var removedEdges = new HashSet<((string v1, string v2), string tag)>();

           

            StringBuilder sb = new StringBuilder();

            // vertices
            sb.Append("Vertices:\n");
            foreach (var v in this.payload.vertices)
            {
                // remove
                if (!removedVertices.Contains(v))
                    sb.Append(v.value + "|");
            }

            // add back
            foreach (var v in addedVertices)
            {
                sb.Append(v.value + "|");
            }

            sb.Append("\nEdges:\n");

            foreach (var e in this.payload.edges)
            {
                string v1 = e.Item1.v1;
                string v2 = e.Item1.v2;

                // remove
                if (lookup(v1) == (null, null) ||
                    lookup(v2) == (null, null) ||
                    removedEdges.Contains(e))
                    continue;
                else
                    sb.Append("<" + v1 + "," + v2 + ">");
            }

            // add back
            foreach (var e in addedEdges)
            {
                sb.Append("<" + e.Item1.v1 + "," + e.Item1.v2 + ">");
            }

            // TODO: wait for client to recognize long strings
            res.AddResponse(Dest.client, "");//sb.ToString());
            return res;
        }

        public override Responses SetValue()
        {
            this.payload = new RGraphPayload(uid);

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client);
            GenerateSyncRes(ref res, "n", "");
            return res;
        }

        public Responses AddVertex()
        {
            string tag = UniqueTag();
            (string, string)v = (this.parameters.GetParam<string>(0), tag);
            this.payload.vertices.Add(v);

            // history
            string opid = "";
            DEBUG("Vertex " + v.Item1 + " with opid " + opid + " added");
            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, "av", v.ToString(), v.Item1+ "," + opid);
            return res;
        }
        
        public Responses RemoveVertex()
        {
            string value = this.parameters.GetParam<string>(0);

            Responses res;

            (string, string) toRemove;

            // precondition
            // collect all unique pairs in V containing v
            if ((toRemove = lookup(value)) == (null, null))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "Vertex DNE");
                return res;
            }

            // v is not the head of an existing arc
            foreach (var item in this.payload.edges)
            {
                if (item.Item1.v1 == value)
                {
                    res = new Responses(Status.fail);
                    res.AddResponse(Dest.client, "Vertex is the head of an existing arc");
                    return res;
                }
            }

            // effect (R)
            this.payload.vertices.Remove(toRemove);

            // history
            string opid =  "";

            res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, "rv", toRemove.ToString());
            return res;

        }

        public Responses AddEdge()
        {
            Responses res;

            string v1 = this.parameters.GetParam<string>(0);
            string v2 = this.parameters.GetParam<string>(1);

            if ((lookup(v1)) == (null, null))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "Head vertex DNE");
                return res;
            }

            // A := A ∪ {((v′, v′′),w)}
            string tag = UniqueTag();
            var e = ((v1, v2), tag);
            this.payload.edges.Add(e);

            // hisotry
            string opid = "";
            res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, "ae", e.ToString());
            return res;
        }

        public Responses RemoveEdge()
        {
            Responses res;

            string v1 = this.parameters.GetParam<string>(0);
            string v2 = this.parameters.GetParam<string>(1);


            ((string, string), string) toRemove;

            if ((toRemove = lookup(v1, v2)) == ((null, null), null))
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "edge DNE");
                return res;
            }

            // A := A \ R
            this.payload.edges.Remove(toRemove);

            // hisotry
            string opid = "";

            res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, "re", toRemove.ToString());
            return res;

        }

        // look up vertex
        private (string, string) lookup(string vertex)
        {

            foreach (var item in this.payload.vertices)
            {
                if (item.value == vertex)
                    return item;
            }

            return (null, null);
        }

        // look up edge
        private ((string, string), string) lookup(string v1, string v2)
        {

            if (lookup(v1) == (null, null) || lookup(v2) == (null, null))
                return ((null, null), null);

            foreach (var item in this.payload.edges)
            {
                if (item.Item1.v1 == v1 && item.Item1.v2 == v2)
                    return item;
            }

            return ((null, null), null);
        }

        public override Responses Synchronization()
        {
            string type = this.parameters.GetParam<string>(0);
            string update = this.parameters.GetParam<string>(1);
            var updateSplit = update.Split(",").Select(x => x.Trim(')', '(', ' ')).ToArray();
            
            string[] vaddopSplit = {};
            if (type == "av")
            {
                string vaddop = this.parameters.GetParam<string>(2);
                vaddopSplit = vaddop.Split(",").Select(x => x.Trim(')', '(', ' ')).ToArray();
            }


            switch (type)
            {
                case "n":
                    this.payload = new RGraphPayload(this.uid);
                    break;
                case "av":
                    var v = (updateSplit[0], updateSplit[1]);
                    this.payload.vertices.Add(v);
                    this.payload.vaddops[vaddopSplit[0]] = vaddopSplit[1];
                    break;
                case "rv":
                    var vremove = (updateSplit[0], updateSplit[1]);
                    this.payload.vertices.Remove(vremove);
                    break;
                case "ae":
                    var e = ((updateSplit[0], updateSplit[1]), updateSplit[2]);
                    this.payload.edges.Add(e);
                    break;
                case "re":
                    var eremove = ((updateSplit[0], updateSplit[1]), updateSplit[2]);
                    this.payload.edges.Remove(eremove);
                    break;
            }


            return new Responses(Status.success);
        }

        private void GenerateSyncRes(ref Responses res, string type, string update, string vaddops = "")
        {   
            Parameters syncPm;
            if (vaddops == "")
                syncPm = new Parameters(2);
            else
                syncPm = new Parameters(3);

            // type: 
            // "n": new graph
            // "av": add vertex
            // "rv": remove vertex
            // "ae": add edge
            // "re": remove edge
            syncPm.AddParam(0, type);
            // effect-update msg
            syncPm.AddParam(1, update);
            if (vaddops != "")
                syncPm.AddParam(2, vaddops);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            res.AddResponse(Dest.broadcast, broadcast, false);
        }


        // TODO: move this to utli class
        public string UniqueTag(int length = TAG_LEN)
        {
            string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(characters[this.payload.random.Next(characters.Length)]);
            }
            return result.ToString();
        }

    }



}

