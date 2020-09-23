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
        self.size = len(self.data)

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

    def test_read(self):
        bad = []

        print("Benching read")
        sample = 0.1

        i = 0

        latency = []
        throughput = []

        start = time.time()
        for k, v in self.data.items():
            lap = time.time()
            res = self.gc.get(k)
            #lap1 = time.time()

            #latency.append(lap1 - lap)

            if (i == int(sample * self.size)):
                print("Throughput at {0} % is {1} ops/s".format(int((sample + 0.001) * 100), i / (lap - start) ))
                #print("Latency at {0} % for write is {1} ms".format(int((sample + 0.001) * 100), 1000 * sum(latency) / len(latency) ) )
                sample += 0.1


            i = i + 1

        for k in bad:
            del data[k]

    def test_mixed_update_read(self, ops):
        print("Benching mixed update/read")

        sample_rate = 0.1

        i = 0
        flag = True

        start = time.time()
        while (i < ops):
            for k, v in self.data.items():
                if (flag):
                    # rotation off incrementing 1 - 10
                    res = self.gc.inc(k, i % 10 + 1)
                    flag = False
                else:
                    res = self.gc.get(k)
                    flag = True

                if (i == int(sample_rate * ops)):
                    lap = time.time()
                    print("Throughput at {0} % is {1} ops/s".format(int((sample_rate + 0.001) * 100), i / (lap - start) ))
                    sample_rate += 0.1


                i = i + 1




class RCounterBench:

    def __init__(self, server, data):
        self.gc = RCounter(server)
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
            res = self.gc.set(k, v)
            #lap1 = time.time()
            #latency.append(lap1 - lap)

            if (i == int(sample * size)):
                lap = time.time()
                print("Throughput at {0} % is {1} ops/s".format(int((sample + 0.001) * 100), i / (lap - start) ))
                #print("Latency at {0} % for write is {1} ms".format(int((sample + 0.001) * 100), 1000 * sum(latency) / len(latency) ) )
                sample += 0.1


            i = i + 1

        for k in bad:
            del data[k]

    def test_read(self):
        bad = []

        print("Benching read")
        size = len(self.data)
        sample = 0.1

        i = 0

        latency = []
        throughput = []

        start = time.time()
        for k, v in self.data.items():
            res = self.gc.get(k)
            #lap1 = time.time()

            #latency.append(lap1 - lap)

            if (i == int(sample * size)):
                lap = time.time()
                print("Throughput at {0} % is {1} ops/s".format(int((sample + 0.001) * 100), i / (lap - start) ))
                #print("Latency at {0} % for write is {1} ms".format(int((sample + 0.001) * 100), 1000 * sum(latency) / len(latency) ) )
                sample += 0.1


            i = i + 1

        for k in bad:
            del data[k]


    def test_mixed_update_read(self, ops):
        print("Benching mixed update/read")

        sample_rate = 0.1

        i = 0
        flag = True

        start = time.time()
        while (i < ops):
            for k, v in self.data.items():
                if (flag):
                    # rotation off incrementing 1 - 10
                    res = self.gc.inc(k, i % 10 + 1)
                    flag = False
                else:
                    res = self.gc.get(k)
                    flag = True

                if (i == int(sample_rate * ops)):
                    lap = time.time()
                    print("Throughput at {0} % is {1} ops/s".format(int((sample_rate + 0.001) * 100), i / (lap - start) ))
                    sample_rate += 0.1


                i = i + 1

    def test_mixed_update_reverse_read(self, ops):
        print("Benching mixed update/read")

        sample_rate = 0.1

        i = 0
        flag = 0

        op_his = {}

        start = time.time()
        while (i < ops):
            for k, v in self.data.items():
                if (flag == 0 or flag == 1):
                    # rotation off incrementing 1 - 10
                    res = self.gc.inc(k, i % 10 + 1)

                    if (res[0]):
                        if k in op_his:
                            op_his[k].append(res[1][0])
                        else:
                            op_his[k] = [res[1][0]]
                elif (flag == 2):
                    opid = random.choice(op_his[k])
                    op_his[k].remove(opid)
                    res = self.gc.rev(k, opid)
                elif (flag == 3):
                    res = self.gc.get(k)

                if (i == int(sample_rate * ops)):
                    lap = time.time()
                    print("Throughput at {0} % is {1} ops/s".format(int((sample_rate + 0.001) * 100), i / (lap - start) ))
                    sample_rate += 0.1
            
                i = i + 1

            if flag == 3:
                flag = 0
            else:
                flag += 1

if __name__ == "__main__":
    kv_pair = generate_kv_pair(1250)
    host1 = "127.0.0.1"
    port1 = 5000
    s = Server(host1, port1)  
    s.connect()

    host2 = "127.0.0.1"
    port2 = 5001
    s2 = Server(host1, port2)  
    s2.connect()

    gcbench = RCounterBench(s, kv_pair)
    gcbench.test_set()
    gcbench.test_mixed_update_reverse_read(1000000)

    s.disconnect()