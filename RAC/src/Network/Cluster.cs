using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NetCoreServer;

using static RAC.Errors.Log;
using System.Net;

namespace RAC.Network
{

    public class NodeCommClient : TcpClient
    {
        public NodeCommClient(IPAddress address, int port) : base(address, port)
        {
            this.OptionSendBufferSize = 1024;
        }
    }



    public class Node
    {
        public int nodeid;

        public string address;

        public int port;

        public bool isSelf = false;

        public NodeCommClient connection;

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
            }
            
            this.address = address;

            if (port <= 0 || port > 65535)
                ERROR("Node " + nodeid + " has an incorrect port number of " + port, new ArgumentOutOfRangeException());

            this.port = port;

            this.connection = new NodeCommClient(IPAddress.Parse(address), port);
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
            // TODO: check if replica id > # of replicas
            
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

        public void connect()
        {   
            
            if (!this.connection.Connect())
            {
                this.connection = null;
                ERROR("Cluster node " + this.address + ":" + this.port + " connection failed");
            }
        }

        public void send(MessagePacket msg)
        {
            Byte[] data = msg.Serialize();
            DEBUG("Sending the following message:\n" + msg);
            if (!this.connection.SendAsync(data, 0, data.Length))
            {
                ERROR("Failure sending Msg: " + msg);
            }
            
        }

        public void disconnect()
        {
            this.connection.Disconnect();
        }


    }

    public class Cluster
    {
        public List<Node> nodes;
        public int numNodes;
        public int numServers;
        public Node selfNode;


        public Cluster(string nodeconfigfile)
        {
            Node.DeserializeNodeConfig(nodeconfigfile, out nodes);
            
            HashSet<string> numServersTemp = new HashSet<string>();

            foreach (var n in nodes)
            {
                if (n.isSelf)
                    selfNode = n;

                // using hasset to count unique IPs
                numServersTemp.Add(n.address);
            }

            this.numNodes = nodes.Count;
            this.numServers = numServersTemp.Count;

        }

        public void ConnectAll()
        {
            foreach (var n in nodes)
            {
                if (!n.isSelf)
                    n.connect();
            }

        }

        public void DisconnectAll()
        {
            foreach (var n in nodes)
            {
                if (!n.isSelf)
                    n.disconnect();
            }
        }

        private const int MAX_RETRY = 5;
        public void BroadCast(MessagePacket msg)
        {   
            // TODO: handle rebroadcast if failed
            foreach (var n in nodes)
            {
                if (n.isSelf)
                    continue;

                int retry = 0;
                while (!n.connection.IsConnected && retry++ <= MAX_RETRY)
                    n.connect();

                if (n.connection is null)
                    ERROR("Broadcast failed to cluster node " + n.address + ":" + n.port);
                else
                {
                    msg.to = n.address.ToString() + ":" + n.port.ToString();
                    n.send(msg);
                }
            }
        }

    }

}