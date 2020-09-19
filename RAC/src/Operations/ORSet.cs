using System;
using System.Collections.Generic;
using System.Text;
using RAC.Payloads;
using static RAC.Errors.Log;

namespace RAC.Operations
{
    public class ORSet : Operation<ORSetPayload>
    {
        private const int TAG_LEN = 8;

        // todo: set this to its typecode
        public override string typecode { get; set; } = "os";

        public ORSet(string uid, Parameters parameters) : base(uid, parameters)
        {
            // todo: put any necessary data here
        }


        public override Responses GetValue()
        {
            Responses res;

            if (this.payload is null)
            {
                res = new Responses(Status.fail);
                res.AddResponse(Dest.client, "Gcounter with id {0} cannot be found");
            }

            var observed = new HashSet<(string value, string tag)>(this.payload.addSet);
            observed.ExceptWith(this.payload.removeSet);

            // construct a list of string
            StringBuilder sb = new StringBuilder();
            foreach (var item in observed)
            {
                sb.Append(item.value + ",");
            }

            res = new Responses(Status.success);
            res.AddResponse(Dest.client, sb.ToString());
            noSideEffect = true;

            return res;
        }

        public override Responses SetValue()
        {
            this.payload = new ORSetPayload(uid);

            Responses res = new Responses(Status.success);

            res.AddResponse(Dest.client);
            GenerateSyncRes(ref res);
            return res;

        }

        public Responses Add()
        {
            string tag = UniqueTag();
            this.payload.addSet.Add((this.parameters.GetParam<string>(0), tag));

            Responses res = new Responses(Status.success);
            res.AddResponse(Dest.client);

            GenerateSyncRes(ref res);

            return res;
        }

        public Responses Remove()
        {
            string value = this.parameters.GetParam<string>(0);


            HashSet<(string, string)> toRemove = new HashSet<(string value, string tag)>();
            foreach (var item in this.payload.addSet)
            {
                if (item.value == value)
                {
                    toRemove.Add(item);
                    this.payload.removeSet.Add(item);

                }
            }

            Responses res;

            if (toRemove.Count > 0)
            {
                res = new Responses(Status.success);
                GenerateSyncRes(ref res);
                this.payload.addSet.ExceptWith(toRemove);

            }
            else
                res = new Responses(Status.fail);

            res.AddResponse(Dest.client);

            return res;
        }

        public HashSet<(string, string)> ConvertToHashSet(List<string> input)
        {
            var res = new HashSet<(string, string)>();
        
            foreach (var item in input)
            {
                var values = item.Split("||");
                res.Add((values[0], values[1]));
            }

            return res;
        }

        public List<string> ConvertToList(HashSet<(string, string)> input)
        {

            var res = new List<String>();

            foreach (var item in input)
                res.Add(item.Item1 + "||" + item.Item2);

            return res;
        }

        public override Responses Synchronization()
        {
            var otherAdd = ConvertToHashSet(this.parameters.GetParam<List<string>>(0));
            var otherRemove = ConvertToHashSet(this.parameters.GetParam<List<string>>(1));

            if (this.payload is null)
            {
                this.payload = new ORSetPayload(uid);
            }


            this.payload.addSet.ExceptWith(otherRemove);
            otherAdd.ExceptWith(this.payload.removeSet);
            this.payload.addSet.UnionWith(otherAdd);

            this.payload.removeSet.UnionWith(otherRemove);

            return new Responses(Status.success);
        }

        private void GenerateSyncRes(ref Responses res)
        {
            var addSetList = ConvertToList(this.payload.addSet);
            var rmSetList = ConvertToList(this.payload.removeSet);

            Parameters syncPm = new Parameters(2);
            syncPm.AddParam(0, addSetList);
            syncPm.AddParam(1, rmSetList);

            string broadcast = Parser.BuildCommand(this.typecode, "y", this.uid, syncPm);

            res.AddResponse(Dest.broadcast, broadcast, false);
        }


        // https://codereview.stackexchange.com/questions/5983/random-string-generation
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

