#!/usr/bin/python3        

import socket
import random
import time
import sys
from type.GCounter import GCounter
from type.RCounter import RCounter
from type.ORSet import ORSet
from type.RGraph import RGraph
from type.Performance import Performance
from type.helper import res_parse

class Server:

    def __init__(self, ip, port):
        self.s = None
        self.ip = ip
        self.port = port 

    def connect(self):
        try:
            self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM) 
            self.s.settimeout(5)
            self.s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.s.connect((self.ip, self.port))
            return 1
        except Exception as e:
            print(e)
            return 0
            

    def response(self):
        try:
            msg = self.s.recv(1024)   
        except socket.timeout:
            print("timeout on receive")
            self.s.close()
            return "F"

        return msg.decode('utf-16')

    def send(self, data):            
        self.s.send(data.encode('utf-16'))
        return res_parse(self.response())

    def disconnect(self):
        self.s.close()        



def isHelp(args):
    return len(sys.argv) == 2 and (args[1] == '--help' or args[1] == '-h')

def helpMessage():
    string = ("  Go to ../RAC and follow the instruction to boot up replication server  \n\n" +
        "  [For Example] python -m http.server 8080 --bind 127.0.0.1 \n\n" +
        "  and in this folder, run: \n\n" +
        "  python client.py 127.0.0.1:<port number> \n\n" + 
        "  [For Example] python client.py 127.0.0.1:<port number> \n")
    print(string)

if __name__ == "__main__":
    if isHelp(sys.argv):
        helpMessage()

    else:
        if len(sys.argv) < 2:
            raise ValueError('wrong arg')
    
        address = sys.argv[1]
        host = address.split(":")[0]
        port = int(address.split(":")[1])

        s = Server(host, port)   

        if s.connect() == 0:
            print("connection failed")
            exit(1)

        while (True):
            text = input("Enter:").split(" ")

            typecode = text[0]

            if (typecode == "x"):
                s.disconnect()
                exit(0)

            uid = text[1]
            opcode = text[2]

            if (typecode == "gc"):
                gc = GCounter(s)
                if (opcode == "g"):
                    print(gc.get(uid))
                if (opcode == "s"):
                    value = text[3]
                    print(gc.set(uid, value))
                if (opcode == "i"):
                    value = text[3]
                    print(gc.inc(uid, value))

            elif (typecode == "rc"):
                rc = RCounter(s)
                if (opcode == "g"):
                    print(rc.get(uid))
                if (opcode == "s"):
                    value = text[3]
                    print(rc.set(uid, value))
                if (opcode == "i"):
                    value = text[3]
                    try:
                        rid = text[4]
                    except:
                        rid = ""
                    print(rc.inc(uid, value, rid))
                if (opcode == "d"):
                    value = text[5]
                    try:
                        rid = text[4]
                    except:
                        rid = ""
                    print(rc.dec(uid, value, rid))
                if (opcode == "r"):
                    value = text[3]
                    print(rc.rev(uid, value))
                    
            elif (typecode == "os"):
                os = ORSet(s)
                if (opcode == "g"):
                    print(os.get(uid))
                if (opcode == "s"):
                    print(os.set(uid))
                if (opcode == "a"):
                    value = text[3]
                    print(os.add(uid, value))
                if (opcode == "rm"):
                    value = text[3]
                    print(os.remvoe(uid, value))

            elif (typecode == "rg"):
                rg = RGraph(s)
                if (opcode == "g"):
                    print(rg.get(uid))
                if (opcode == "s"):
                    print(rg.set(uid))
                if (opcode == "av"):
                    value = text[3]
                    print(rg.addvertex(uid, value))
                if (opcode == "rv"):
                    value = text[3]
                    print(rg.remvoevertex(uid, value))
                if (opcode == "ae"):
                    value1 = text[3]
                    value2 = text[4]
                    print(rg.addedge(uid, value1, value2))
                if (opcode == "re"):
                    value1 = text[3]
                    value2 = text[4]
                    print(rg.removeedge(uid, value1, value2))
                if (opcode == "r"):
                    value = text[3]
                    print(rg.reverse(uid, value))

            elif (typecode == "pref"):
                pf = Performance(s)
                if (opcode == "g"):
                    print(pf.get())

    
        s.disconnect()


    




        


   