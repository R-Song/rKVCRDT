import sys
import json
import socket
import os
import subprocess
import time
import math
from pathlib import Path


# remote:
# run startservers.py separately on each server (start, stop)
# start server remote use ssh to call startservers.py on all servers



SERVER_PATH = str(Path(__file__).resolve().parent.parent.parent) + "/RAC"
REMOTE_SCRIPT_PATH = "/home/ubuntu/Project_RAC/RACClient/src/startservers.py"
START_PORT = 5000

SSH_KEY_FILE = "cc.pem"


def each_server_json(node_id: int, num_per_server: int, servers_list: list , print_addr: bool = False) -> str:
    res = []
    addresses = []

    selfip = socket.gethostbyname(socket.gethostname())

    i = 0
    for ip in servers_list:
        for _ in range(num_per_server):
            isself = False
            if (i == node_id and ip == selfip):
                isself = True

            cfg = {
                "nodeid": i,
                "address": ip,
                "port": START_PORT + i,
                "isSelf": isself
            }
            res.append(cfg)
            addresses.append(selfip + ":" + str(START_PORT + i))
            i += 1

    if print_addr:
        print("Server addresses:")
        print(addresses)

    return json.dumps(res), addresses


def generate_json(num_per_server, servers_list) -> list:

    # local ip
    selfip = socket.gethostbyname(socket.gethostname())

    if servers_list == []:
        servers_list = [selfip]

    i = 0
    for ip in servers_list:
        for _ in range(num_per_server):
            if ip == selfip:
                cfg_json, addresses = each_server_json(i, num_per_server, servers_list, i == 0)
                f = open("cluster_config." + str(i) + ".json", "w")
                f.write(cfg_json)
                f.close()
            i += 1

    return addresses


def start_server(num_server, servers_list = []) -> list:
    addresses = generate_json(num_server, servers_list)
    cwd = os.getcwd()
    ftemp = open("temp.txt", "w")
    print("Server started at pid:")
    for i in range(num_server):
        cfg = cwd + "/cluster_config." + str(i) + ".json"
        flog = open("log." + str(i) + ".txt", "w")
        proc = subprocess.Popen(
            ["dotnet", "run", "-p", SERVER_PATH, cfg], stdout=flog, stderr=flog)
        pid = str(proc.pid)
        print(pid)
        ftemp.write(pid + "\n")
        time.sleep(1)

    ftemp.close()

    return addresses


def start_server_remote(num_server, servers_list) -> list:
    res = []
    for ip in servers_list:
        proc = subprocess.run(
            ["ssh", "-i", "-p", SSH_KEY_FILE, "ubuntu@" + ip, "python3 " + REMOTE_SCRIPT_PATH + " start " + str(num_server) + " " + servers_list], stdout=subprocess.PIPE)

        print(proc.stdout)

        res.append(str(proc.pid))
        
    
def stop_server_remote(servers_list, list_pids):
    for ip in servers_list:
        proc = subprocess.run(
            ["ssh", "-i", "-p", SSH_KEY_FILE, "ubuntu@" + ip, "python3 " + REMOTE_SCRIPT_PATH + " stop"])

    


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
        raise ValueError(
            'Wrong action, Usage: StartServers.py [start/stop/restart/rstart] [number_of_servers]')

    if (action == "start"):
        try:
            num_server = int(sys.argv[2])
        except Exception:
            raise ValueError(
                'Need number of server, Usage: StartServers.py start [number_of_servers]')
        start_server(num_server)

    elif (action == "rstart"):
        try:
            num_pre_server = int(sys.argv[2])
            host_ips = sys.argv[3].split(',')
        except Exception:
            raise ValueError(
                'Need number of server, Usage: StartServers.py rstart [number_pre_servers] [ip1, ip2]')
        start_server(num_pre_server, host_ips)

    elif (action == "stop"):
        stop_server()
    elif (action == "restart"):
        restart_server()
    else:
        raise ValueError(
            'Wrong action, Usage: StartServers.py [start/stop/restart] [number_of_servers]')
