#!/usr/bin/python3        

import socket
import random
import time
import sys

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
            msg = self.s.recv(256)   
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

rac_starter = "-RAC-\n"
rac_ender = "\n-EOF-"
client_addr = "Client"

def msg_construct(server, msg):
    s = rac_starter + \
        "FROM:" + client_addr + "\n" + \
        "TO:" + str(server.ip) + ":" + str(server.port) + "\n" + \
        "CLS:" + "c\n" + \
        "LEN:" + str(len(msg)) + "\n" + \
        "CNT:\n" + msg + str(rac_ender)

    return s

typePrefix = "TYPE:"
uidPrefix = "UID:"
opPrefix = "OP:"
paramPrefix = "P:"

def req_construct(tid, uid, op, params):
    req = typePrefix + tid + "\n" + \
          uidPrefix + uid + "\n" + \
          opPrefix + op + "\n"

    for p in params:
        req += paramPrefix + p + "\n" 

    return req

def res_parse(res):
    
    # TODO: add try for timeout
    lines = res.split("CNT:")[1].strip().strip('-EOF-').splitlines()

    if "Succeed" in lines[0]:
        success = True 
    else:
        success = False

    del lines[0]

    return (success, lines)

class GCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("gc", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = req_construct("gc", id, "s", [str(value)])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def inc(self, id, value):
        req = req_construct("gc", id, "i", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res


class RCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("rc", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = req_construct("rc", id, "s", [str(value)])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def inc(self, id, value, rid = ""):
        req = req_construct("rc", id, "i", [str(value), rid]) 
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res


    def dec(self, id, value, rid = ""):
        req = req_construct("rc", id, "d", [str(value), rid]) 
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def rev(self, id, value):
        req = req_construct("rc", id, "r", [str(value)]) 
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res


class ORSet:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("os", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id):
        req = req_construct("os", id, "s", [])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def add(self, id, value):
        req = req_construct("os", id, "a", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def remvoe(self, id, value):
        req = req_construct("os", id, "rm", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res


class RGraph:
    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("rg", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id):
        req = req_construct("rg", id, "s", [])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def addvertex(self, id, value):
        req = req_construct("rg", id, "av", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def remvoevertex(self, id, value):
        req = req_construct("rg", id, "rv", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def addedge(self, id, value1, value2):
        req = req_construct("rg", id, "ae", [str(value1), str(value2)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def removeedge(self, id, value1, value2):
        req = req_construct("rg", id, "re", [str(value1), str(value2)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def reverse(self, id, value):
        req = req_construct("rg", id, "r", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

class Performance:
    def __init__(self, s):
        self.server = s

    def get(self):
        req = req_construct("pref", "pf", "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res



if __name__ == "__main__":
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




        


   