# Reversible CRDT

## How to run the servers:
1. Set-up .Net (.Net 5): https://dotnet.microsoft.com/download
2. Go to `Project_RAC/RAC/`
3. For #n of nodes you want, make n copies of `cluster_config_example.json`, where in each of the copy, make n copies of 

```
{
        "nodeid": [node id, from 0 to n], 
        "address": [ip address],
        "port": [port for this node],
        "isSelf": true [only one is true, corresponding to the one the server is taking as input]
}
```

elements.,

4. Use `dotnet run cluster_config_example.[node_id].json` or the binary to run an instance of sever as a node


## How to use the client
1. Install python 3.8.5+
2. Go to `Project_RAC/RACClient/src/`
3. Run `client.py [server_ip:port]` to connect a node(use `python3 client.py [server_ip:port]` if its not working.
4. A commandline UI will show up, type in command to interact with the server
5. Commands are the following format
[typecode] [key] [opcode] [value1] [value2]...
6. input `x` to disconnect

Type supported:
PN-Counter: typecode `pnc`
  set `s [initial_value]`
  get `r`
  increment `i [number_to_add]`
  decrement `d [number_to_subtract]`
  
OR-Set: typecode `os`
Graph: typecode `g`
and their reversible counterpart

see https://github.com/yunhaom94/Project_RAC/blob/master/RAC/src/API.cs for more infomation

Open clients to different nodes at the same time to see how replication works!


