using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

using static RAC.Errors.Log;

namespace RAC.Network
{
    public class Node
    {
        public int nodeid;

        public string address;

        public int port;

        public bool isSelf = false;

        [JsonConstructor]
        public Node(int nodeid, string address, int port)
        {
            this.nodeid = nodeid;

            try 
            {
                IPAddress.Parse(address);
            }
            catch(FormatException e)
            {
                ERROR("Node " + nodeid + " has an incorrect ip address", e);
                throw e;
            }
            
            this.address = address;

            if (port <= 0 || port > 65535)
                ERROR("Node " + nodeid + " has an incorrect port number of " + port, new ArgumentOutOfRangeException());

            this.port = port;
        }

        public override string ToString() 
        {
            return "Node " + nodeid + ": Address: " + this.address + ":" + this.port + ", is self? " + this.isSelf;
        }

        public static bool DeserializeNodeConfig(string filename, out List<Node> nodes)
        {
            nodes = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(filename));

            // sanity check
            // check if multiple selves
            int selfNodeCount = 0;
            // check if duplicate nodes
            HashSet<string> addrportSet = new HashSet<string>();
            
            foreach (var n in nodes)
            {
                if (n.isSelf)
                    selfNodeCount++;

                if (selfNodeCount > 1)
                {
                    ERROR("Config: Too many self node!");
                    return false;
                }

                string addrport = n.address + n.port.ToString();
                if (addrportSet.Contains(addrport))
                    ERROR("Duplicate nodes!");
                else
                    addrportSet.Add(addrport);
            }

            if (selfNodeCount == 0)
            {
                ERROR("Config: No self node");
                return false;
            }

            StringBuilder listingNodes = new StringBuilder("The following nodes are initalized:\n");
            foreach (var n in nodes)
                listingNodes.AppendLine(n.ToString());

            LOG(listingNodes.ToString());
            return true;

        }
    }

    public class Cluster
    {
        public List<Node> nodes;
        public int numNodes;
        public Node selfNode;

        public Cluster(string nodeconfigfile)
        {
            Node.DeserializeNodeConfig(nodeconfigfile, out nodes);
            
            foreach (var n in nodes)
            {
                if (n.isSelf)
                    selfNode = n;
            }

            numNodes = nodes.Count;

        }


    }

}