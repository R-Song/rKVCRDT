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
    s = rac_starter + client_addr + "\n" + \
        str(server.ip) + ":" + str(server.port) + "\n" + \
        str(len(msg)) + "\n" + \
        msg + str(rac_ender)

    return s

class GCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = "gc\n" + \
              str(id) + "\n" + \
              "g\n"

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




if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise ValueError('wrong arg')
    
    address = sys.argv[1]
    host = address.split(":")[0]
    port = int(address.split(":")[1])

    
    s = Server(host, port)   
          
    gc = GCounter(s)

    print(gc.set(9, 20))
    print(gc.get(9))
    
    print(gc.inc(9, 14))
    print(gc.get(9))


        




        


   