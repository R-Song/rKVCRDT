#!/usr/bin/python3        

import socket
import random
import time
import sys


def generate_keys(num_elements):
    res = []
    for i in range(num_elements):
        res.append(str(int(random.uniform(1000000, 9999999))))

    return res

def generate_values(num_elements):
    res = []
    for i in range(num_elements):
        res.append(int(random.uniform(1,100)))

    return res

def generate_kv_pair(num_elements):
    res = {}

    keys = generate_keys(num_elements)
    values = generate_values(num_elements)

    for i in range(num_elements):
        res[keys[i]] = values[i]

    return res


class Server:

    def __init__(self, ip, port):
        self.s = None
        self.ip = ip
        self.port = port 

    def connect(self):
        try:
            self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM) 
            self.s.settimeout(5)
            self.s.connect((self.ip, self.port))
            return 1
        except:
            return 0
            

    def response(self):
        try:
            msg = self.s.recv(256)   
        except socket.timeout:
            print("timeout on receive")
            self.s.close()
            return "F"

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

class GCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = "gc\n" + \
              str(id) + "\n" + \
              "g"

        req = msg_construct(self.server, req)

        
        self.server.connect()
        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = "gc\n" + \
              str(id) + "\n" + \
              "s\n" + \
              str(value)

        req = msg_construct(self.server, req)
        
        self.server.connect()
        res = self.server.send(req)
        return res

    def inc(self, id, value):
        req = "gc\n" + \
              str(id) + "\n" + \
              "i\n" + \
              str(value)

        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res


class RCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = "rc\n" + \
              str(id) + "\n" + \
              "g"

        req = msg_construct(self.server, req)

        
        self.server.connect()
        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = "rc\n" + \
              str(id) + "\n" + \
              "s\n" + \
              str(value)

        req = msg_construct(self.server, req)
        
        self.server.connect()
        res = self.server.send(req)
        return res

    def inc(self, id, value):
        req = "rc\n" + \
              str(id) + "\n" + \
              "i\n" + \
              str(value)

        req = msg_construct(self.server, req)

        self.server.connect()
        res = self.server.send(req)
        return res


    def dec(self, id, value):
        req = "rc\n" + \
              str(id) + "\n" + \
              "d\n" + \
              str(value)

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
            print(rc.inc(uid, value))
        if (opcode == "d"):
            value = sys.argv[5]
            print(rc.dec(uid, value))


    
        




        


   