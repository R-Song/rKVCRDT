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



class Test:
    def __init__(self, addresses, num_element, CRDT, sample_rate=0.1):
        self.servers = []
        for ip, port in addresses.items():
            s = Server(ip, port)
            s.connect()
            self.servers.append(s)

        self.data  = generate_kv_pair(num_element)
        self.keys = self.data.keys()
        self.sample_rate = sample_rate

        self.crdts = []
        self.prefs = []
        
        for i in range(len(self.servers)):
            self.crdts.append(CRDT(self.servers[i]))
            self.prefs.append(Performance(self.servers[i]))

       

    def init_data(self):
        print("Initialize data with size of {0}".format(len(self.data)))

        sample_point = 0
        start = time.time()
        i = 0

        for k, v in self.data.items():
            
            res = self._set(k, v)
            self._check_success(res)

            sample_point = self._throughout(start, i, len(self.data), sample_point)
            i = i + 1

    def test_data(self):
        print("Verifying initialized data by reading all data")

        for k, v in self.data.items():
            res = self._read(k, v)
            if not self._check_success(res):
                return False
            

        print("Verifying complete")
        return True

    def _set(self, k, v):
        raise NotImplementedError

    def _read(self, k, v):
        raise NotImplementedError

    def _throughout(self, start_time, num_ops, total_ops, sample_point):
        if (num_ops == int(sample_point * total_ops)):
                cur_t = time.time()
                tp = num_ops / (cur_t + 0.00001 - start_time)
                print("Throughput at {0} % is {1} ops/s".format(int(sample_point * 100), int(tp)))
                self._pref()
                return sample_point + self.sample_rate
        
        return sample_point
    
    def _pref(self):
        for p in self.prefs:
            res = p.get()
            print("Node: {0}".format(res[1]))


    def _check_success(self, res):
        if not res[0]:
            print("WARNING: OP FAILED WITH RES {0}".format(res))
            return False
        else:
            return True

    def _do_actions(self, actions, num_actions):
        raise NotImplementedError

    def _do_actions_intermix(self, actions):
        raise NotImplementedError


    def end(self):
        for s in self.servers:
            s.disconnect()


class GCounterTest(Test):
    def __init__(self, addresses, num_element, sample_rate=0.1) -> None:
        super().__init__(addresses, num_element, GCounter, sample_rate)

    def _set(self, k, v):
        return self.crdts[0].set(k, v)

    def _read(self, k, v):
        return self.crdts[0].get(k)

    def test_mixed_update_read(self, read_ratio, num_ops):

        sample_point = 0
        start = time.time()
        i = 0

        while(i < num_ops):
            k = random.choice(list(self.data.keys()))
            v = random.uniform(0, 1)
            
            # update
            if (v > read_ratio):
                res = self.crdts[0].inc(k, int(v * 10))
            else:
                res = self.crdts[0].get(k)

            sample_point = self._throughout(start, i, num_ops, sample_point)
            i = i + 1

class GraphTest(test):
    



if __name__ == "__main__":
    # kv_pair = generate_kv_pair(1250)
    # host1 = "127.0.0.1"
    # port1 = 5000
    # s = Server(host1, port2)  
    # s.connect()

    # host2 = "127.0.0.1"
    # port2 = 5001
    # s2 = Server(host1, port2)  
    # s2.connect()

    # gcbench = RCounterBench(s, kv_pair)
    # gcbench.test_set()
    # gcbench.test_mixed_update_reverse_read(1000000)

    # s.disconnect()
    gctest = GCounterTest({"127.0.0.1": 5000}, 10000)
    gctest.init_data()
    gctest.test_data()
    gctest.test_mixed_update_read(0.5, 100000)
    gctest.end()