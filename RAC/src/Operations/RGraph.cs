using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RAC.History;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    /// <summary>
    /// Reversible Graph wit selective reversibility:
    /// All add edge relates to the operation of adding the head vertex
    /// Thus:
    /// Reverse add vertex: remove all edges with it's as head
    /// Reverse remove vertex: add back the vertex
    /// Reverse add edge: remove the edge
    /// Reverse remove edge: add the edge
    /// </summary>
    public class RGraph : Operation<RGraphPayload>
    {
        private const int TAG_LEN = 8;

        // todo: set this to its typecode
        public override string typecode { get; set; } = "rg";

        public RGraph(string uid, Parameters parameters) : base(uid, parameters)
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

            foreach (var opid in this.history.tombstone)
            {
                // first handle the op
                var op = this.history.log[opid];

                HandleSavedState(op, addedVertices, removedVertices, addedEdges, removedEdges);
                // then handle the related
                foreach (var relate_opid in op.related)
                {
                    op = this.history.log[opid];
                    HandleSavedState(op, addedVertices, removedVertices, addedEdges, removedEdges);
                }
            }

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

            res.AddResponse(Dest.client, sb.ToString());
            return res;
        }

        private void HandleSavedState(in OpEntry op,
                                    in HashSet<(string, string)> addedVertices,
                                    in HashSet<(string, string)> removedVertices,
                                    in HashSet<((string, string), string)> addedEdges,
                                    in HashSet<((string, string), string)> removedEdges)
        {
            var beforeTemp = op.before.Split(",").Select(x => x.Trim(')', '(', ' ')).ToArray();
            var afterTemp = op.after.Split(",").Select(x => x.Trim(')', '(', ' ')).ToArray();

            // remove all after
            // add all before
            // if have 2 strings, then is vertex value-tag
            // if exists before but not after, add back 
            // if exists after but not before, remove
            // adding back dont care related, because original CRDT semantically frobit removing head vertices
            if (beforeTemp.Length == 2)
            {
                addedVertices.Add((beforeTemp[0], beforeTemp[1]));
            }
            else if (afterTemp.Length == 2)
            {
                removedVertices.Add((afterTemp[0], afterTemp[1]));
            }

            // if have 2 strings, then is edge (v1-v2)-tag
            if (beforeTemp.Length == 3)
            {
                addedEdges.Add(((beforeTemp[0], beforeTemp[1]), beforeTemp[2]));
            }
            else if (afterTemp.Length == 3)
            {
                removedEdges.Add(((afterTemp[0], afterTemp[1]), afterTemp[2]));
            }

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
            var v = (this.parameters.GetParam<string>(0), tag);
            this.payload.vertices.Add(v);

            // history
            string opid = this.history.AddNewEntry("", v.ToString());
            
            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client, opid);
            GenerateSyncRes(ref res, "av", v.ToString());
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
            string opid = this.history.AddNewEntry(toRemove.ToString(), "");

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
            string opid = this.history.AddNewEntry("", e.ToString());
            this.history.addRelated(this.payload.vaddops[v1], opid);

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
            string opid = this.history.AddNewEntry(toRemove.ToString(), "");
            this.history.addRelated(this.payload.vaddops[v1], opid);

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

            switch (type)
            {
                case "n":
                    this.payload = new RGraphPayload(this.uid);
                    break;
                case "av":
                    var v = (updateSplit[0], updateSplit[1]);
                    this.payload.vertices.Add(v);
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

        private void GenerateSyncRes(ref Responses res, string type, string update)
        {
            Parameters syncPm = new Parameters(2);

            // type: 
            // "n": new graph
            // "av": add vertex
            // "rv": remove vertex
            // "ae": add edge
            // "re": remove edge
            syncPm.AddParam(0, type);
            // effect-update msg
            syncPm.AddParam(1, update);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);
            res.AddResponse(Dest.broadcast, broadcast, false);
        }


        public Responses Reverse()
        {
            string opid = this.parameters.GetParam<string>(0);
            this.history.addTombstone(opid);

            return new Responses(Status.success);
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

