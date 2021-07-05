import sys
import json
import socket
import os
import subprocess
import time
import psutil
from pathlib import Path

SERVER_PATH = str(Path(__file__).resolve().parent.parent.parent) + "\RAC"
START_PORT = 5000

def each_server_json(node_id, num_server, print_addr = False) -> str:
    res = []
    # local ip
    ip = socket.gethostbyname(socket.gethostname())
    addresses = []

    for i in range(num_server):
        isself = False
        if (i == node_id):
            isself = True

        cfg =  {
        "nodeid": i, 
        "address": ip,
        "port": START_PORT + i,
        "isSelf": isself
        }
        res.append(cfg)
        addresses.append(ip + ":" + str(START_PORT + i))

    if print_addr:
        print("Server addresses:")
        print(addresses)

    return json.dumps(res), addresses



def generate_json(num_server) -> list:
    
    for i in range(num_server):
        
        cfg_json, addresses = each_server_json(i, num_server, i == 0)
        f = open("cluster_config." + str(i) + ".json", "w")
        f.write(cfg_json)
        f.close()

    return addresses


def start_server(num_server) -> list:
    addresses = generate_json(num_server)
    cwd = os.getcwd()
    ftemp = open("temp.txt", "w")
    print("Server started at pid:")
    for i in range(num_server):
        cfg = cwd + "/cluster_config." + str(i) + ".json"
        flog = open("log." + str(i) + ".txt", "w")
        proc = subprocess.Popen(["dotnet", "run", "-p", SERVER_PATH, cfg], stdout=flog, stderr=flog)
        pid = str(proc.pid)
        print(pid)
        ftemp.write(pid + "\n")
        time.sleep(1)

    ftemp.close()

    return addresses


def stop_server():
    # delete json files
    import signal

    try:
        with open("temp.txt", "r") as ftemp:
            pid = int(ftemp.readline())
            print("Server stopped with pid:")
            while(pid):
                print(pid)
                try:
                    os.kill(pid, signal.SIGTERM)  
                    print(pid)
                except OSError:
                    continue
                finally:
                    try:
                        pid = int(ftemp.readline())
                    except ValueError:
                        break

                
                
    except FileNotFoundError:
        raise IndentationError("Servers are not started!")


    ftemp.close()

    os.remove(os.getcwd() + "/temp.txt")

    files = os.listdir(os.getcwd())
    for f in files:
        filename = os.path.splitext(f)[0]
        extension = os.path.splitext(f)[1]

        if extension == ".json" and filename[0:7] == "cluster":
            os.remove(f)

def restart_server():
    try:
        with open("temp.txt", "r") as ftemp:
            i = 0
            while(ftemp.readline()):
                i += 1

        stop_server()
        start_server(i)

                
    except FileNotFoundError:
        raise IndentationError("Servers are not started!")


if __name__ == "__main__":        
    try:
        action = sys.argv[1]
    except Exception:
        raise ValueError('Wrong action, Usage: StartServers.py [start/stop/restart] [number_of_servers]')
    

    if (action == "start"):
        try:
            num_server = int(sys.argv[2])
        except Exception:
            raise ValueError('Need number of server, Usage: StartServers.py [start/stop/restart] [number_of_servers]')
        start_server(num_server)
    elif (action == "stop"):
        stop_server()
    elif (action == "restart"):
        restart_server()
    else:
        raise ValueError('Wrong action, Usage: StartServers.py [start/stop/restart] [number_of_servers]')
    


