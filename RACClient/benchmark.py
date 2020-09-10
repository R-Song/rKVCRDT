import string 
import random 
import time
from client import *

KEY_LEN = 5

def rand_str(n):
    return ''.join(random.choices(string.ascii_uppercase +
                             string.digits, k = n)) 


def generate_keys(num_elements, key_len):
    res = []
    for i in range(num_elements):
        res.append(rand_str(key_len))

    return res

def generate_values(num_elements):
    res = []
    for i in range(num_elements):
        res.append(int(random.uniform(1,100)))

    return res

def generate_kv_pair(num_elements):
    res = {}

    keys = generate_keys(num_elements, KEY_LEN)
    values = generate_values(num_elements)

    for i in range(num_elements):
        res[keys[i]] = values[i]

    return res

class GCounterBench:

    def __init__(self, server, data):
        self.gc = GCounter(server)
        self.data = data
        self.keys = data.keys()
        self.values = list(data.values())

    def test_set(self):
        bad = []

        print("Benching pure write")
        size = len(self.data)
        sample = 0.1

        i = 0

        latency = []
        throughput = []

        start = time.time()
        for k, v in self.data.items():
            lap = time.time()
            res = self.gc.set(k, v)
            #lap1 = time.time()

            #latency.append(lap1 - lap)

            if (i == int(sample * size)):
                print("Throughput at {0} % is {1} ops/s".format(int((sample + 0.001) * 100), i / (lap - start) ))
                #print("Latency at {0} % for write is {1} ms".format(int((sample + 0.001) * 100), 1000 * sum(latency) / len(latency) ) )
                sample += 0.1


            i = i + 1

        for k in bad:
            del data[k]

if __name__ == "__main__":
    kv_pair = generate_kv_pair(10000)
    host = "127.0.0.1"
    
    port = 5000
    s = Server(host, port)  
    gcbench = GCounterBench(s, kv_pair)
    gcbench.test_set()