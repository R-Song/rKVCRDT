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

        self.s.shutdown(socket.SHUT_RDWR)
        self.s.close()        
        return msg.decode('utf-16')

    def send(self, data):
        if self.connect() == 0:
            print("connection failed")
            return "F"
            
        self.s.send(data.encode('utf-16'))
        return self.response()

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

class GCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("gc", id, "g", [])
        req = msg_construct(self.server, req)

        
        self.server.connect()
        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = req_construct("gc", id, "s", [str(value)])
        req = msg_construct(self.server, req)
        
        self.server.connect()
        res = self.server.send(req)
        return res

    def inc(self, id, value):
        req = req_construct("gc", id, "i", [str(value)])
        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res


class RCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("rc", id, "g", [])
        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = req_construct("rc", id, "s", [str(value)])
        req = msg_construct(self.server, req)
        
        self.server.connect()
        res = self.server.send(req)
        return res

    def inc(self, id, value, rid):
        req = req_construct("rc", id, "i", [str(value), rid]) 
        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res


    def dec(self, id, value, rid):
        req = req_construct("rc", id, "d", [str(value), rid]) 
        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res

    def rev(self, id, value):
        req = req_construct("rc", id, "r", [str(value)]) 
        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res


if __name__ == "__main__":
    if len(sys.argv) < 4:
        raise ValueError('wrong arg')
    
    address = sys.argv[1]
    host = address.split(":")[0]
    port = int(address.split(":")[1])

    typecode = sys.argv[2]
    uid = sys.argv[3]
    opcode = sys.argv[4]

    s = Server(host, port)   

    if (typecode == "gc"):
        gc = GCounter(s)
        if (opcode == "g"):
            print(gc.get(uid))
        if (opcode == "s"):
            value = sys.argv[5]
            print(gc.set(uid, value))
        if (opcode == "i"):
            value = sys.argv[5]
            print(gc.inc(uid, value))
    elif (typecode == "rc"):
        rc = RCounter(s)
        if (opcode == "g"):
            print(rc.get(uid))
        if (opcode == "s"):
            value = sys.argv[5]
            print(rc.set(uid, value))
        if (opcode == "i"):
            value = sys.argv[5]
            try:
                rid = sys.argv[6]
            except:
                rid = ""
            print(rc.inc(uid, value, rid))
        if (opcode == "d"):
            value = sys.argv[5]
            try:
                rid = sys.argv[6]
            except:
                rid = ""
            print(rc.dec(uid, value, rid))
        if (opcode == "r"):
            value = sys.argv[5]
            print(rc.rev(uid, value))

    
        




        


   