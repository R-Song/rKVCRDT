import string 
import random 
import time
import subprocess
from enum import Enum
from client import *
from draw import *
from multiprocessing import Process


KEY_LEN = 5
STR_LEN = 10

class VAR_TYPE(Enum):
    INT = 1
    STRING = 2

def rand_str(n):
    return ''.join(random.choices(string.ascii_uppercase +
                             string.digits, k = n)) 


def generate_keys(num_elements, key_len):
    res = []
    for i in range(num_elements):
        res.append(rand_str(key_len))

    return res

def generate_values(num_elements, values_types = VAR_TYPE.INT):
    res = []
    for i in range(num_elements):
        if values_types == VAR_TYPE.INT:
            res.append(int(random.uniform(1,100)))
        elif values_types == VAR_TYPE.STRING: 
            res.append(rand_str(STR_LEN))

    return res

def generate_kv_pair(num_elements):
    res = {}

    keys = generate_keys(num_elements, KEY_LEN)
    values = generate_values(num_elements)

    for i in range(num_elements):
        res[keys[i]] = values[i]

    return res

def split_ipport(address):

    res = address.split(":")

    return res[0], int(res[1])

class ExperimentData():
    def __init__(self, num_objects, keys = []) -> None:
        self.num_objects = num_objects
        self.keys = keys if len(keys) > 0 else self._generate_keys()

    def CRDT(self, server):
        raise NotImplementedError

        
    def generate_op_values(self, num_ops, ops_ratio):
        raise NotImplementedError

    def _generate_keys(self):
        res = []
        for i in range(self.num_objects):
            res.append(rand_str(KEY_LEN))

        return res

    def _generate_values(self, num_ops, val_type):
        res = []
        for i in range(num_ops):
            if val_type == VAR_TYPE.INT:
                res.append(int(random.uniform(1,100)))
            elif val_type == VAR_TYPE.STRING: 
                res.append(rand_str(STR_LEN))

        return res

    def _generate_ops(self, num_ops, ops_ratio, op_types):
        res = []
        if round(sum(ops_ratio)) != 1 or len(ops_ratio) != len(op_types):
            print("Ratio error:" + str(ops_ratio))
            raise ValueError

        slots = [0]
        for r in ops_ratio:
            slots.append(slots[-1] + r)

        for _ in range(num_ops):
            sample = random.uniform(0,1)
            for i in range(len(slots)):
                if sample > slots[i] and sample <= slots[i+1]:
                    res.append(op_types[i])

        return res

class GCExperimentData(ExperimentData):

    def CRDT(self, server):
        return GCounter(server)

    def generate_op_values(self, num_ops, ops_ratio):
        res = []
        values = self._generate_values(num_ops, VAR_TYPE.INT)
        ops = self._generate_ops(num_ops, ops_ratio, ["i", "g"])
        assert len(ops) == len(values)
        kidx = 0
        for i in range(len(values)):
            k = self.keys[kidx]
            v = values[i]
            op = ops[i]
            res.append((op, k, v))
            kidx += 1
            if (kidx == len(self.keys)):
                kidx = 0

        return res

    def op_execute(self, crdt, req):
        op = req[0]
        key = req[1]
        v = req[2]
        if op == "g":
            res = crdt.get(key)
        elif op == "s":
            res = crdt.set(key, v)
        elif op == "i":
            res = crdt.inc(key, v)

        return res


class TestRunner():
    def __init__(self, nodes, multiplier, data) -> None:
        self.nodes = nodes
        self.num_nodes = len(nodes)
        self.num_clients = self.num_nodes * multiplier
        self.data = data
        self.connections = self._connect()
        self.crdts = [self.data.CRDT(s) for s in self.connections]

    def _connect(self):
        res = []
        for adddress in self.nodes:
            ip, port = split_ipport(adddress)
            s = Server(ip, port)
            s.connect()
            res.append(s)

        return res


    def worker(self, crdt, list_reqs):
        for req in list_reqs:
            self.data.op_execute(crdt, req)


    def init_data(self):
        nidx = 0
        for key in self.data.keys:
            self.crdts[nidx].set(key, nidx)
            nidx += 1
            if (nidx == len(self.crdts)):
                nidx = 0

    def prep_ops(self, total_prep_ops, pre_ops_ratio):
        pass

    def peak_throughput_benchmark(self):
        pass


def f(name):
    while True:
        i = 809809238482402340 / 32084240923480923



        
if __name__ == "__main__":
    print("test")

    # p = Process(target=f, args=('bob',))
    # p2 = Process(target=f, args=('bob',))
    # p.start()  
    # p2.start()
    # p.join()

    total_objects = 5
    total_ops = 10
    td = GCExperimentData(total_objects)
    # #td = TestData(RCounter, total_objects, total_ops, [0.25, 0.25, 0.3, 0.2])
    print(td.keys)
    #print(td.generate_op_values(total_ops, [0.5, 0.5]))

    tr = TestRunner(["127.0.0.1:5000", "127.0.0.1:5001"],1, td)
    tr.init_data()




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

    def _throughout(self, start_time, num_ops, total_ops, sample_point, tpo=[0]):
        if (num_ops == int(sample_point * total_ops)):
                cur_t = time.time()
                tp = num_ops / (cur_t + 0.00001 - start_time)
                print("Throughput at {0} % is {1} ops/s".format(int(sample_point * 100), int(tp)))
                self._pref()
                tpo[0] = tp
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
        
        all = []
        while(i < num_ops):
            k = random.choice(list(self.data.keys()))
            v = random.uniform(0, 1)
            
            # update
            if (v > read_ratio):
                res = self.crdts[0].inc(k, int(v * 10))
            else:
                res = self.crdts[0].get(k)

            temp = [0]
            sample_point = self._throughout(start, i, num_ops, sample_point, temp)
            if (temp[0] != 0):
                all.append(temp[0])
            i = i + 1

        return all

class RGraphTest(Test):
    def __init__(self, addresses, num_element, sample_rate=0.1) -> None:
        super().__init__(addresses, num_element, RGraph, sample_rate)

    def _set(self, k, v):
        return self.crdts[0].set(k)

    def _read(self, k, v):
        return self.crdts[0].get(k)

    def set_reverse(self, num_reversed):
        print("setting up reversible")
        sample_point = 0
        start = time.time()
        i = 0

        vertices = []
        edges = []

        for r in range(num_reversed):
            vertices.append("v{0}".format(r))
            edges.append(("v{0}".format(r), "v{0}".format(r + 1)))
        
        edges.pop()

        num_run = len(self.data.items()) * num_reversed * 3
        for k, v in self.data.items():
            opids = []
            # add
            for vt in vertices:
                res = self.crdts[0].addvertex(k, vt)
                opids.append(res[1][0])
                sample_point = self._throughout(start, i, num_run, sample_point)
                i = i + 1

            for ed in edges:
                self.crdts[0].addedge(k, ed[0], ed[1])
                sample_point = self._throughout(start, i, num_run, sample_point)
                i = i + 1

            # reverse
            for opid in reversed(opids):
                self.crdts[0].reverse(k, opid)
                
                sample_point = self._throughout(start, i, num_run, sample_point)
                i = i + 1

                

    
    def test_mixed_update_read(self, read_ratio, num_ops):
        print("testing mixed update")
        sample_point = 0
        start = time.time()
        i = 0

        while(i < num_ops):
            k = random.choice(list(self.data.keys()))
            v = random.uniform(0, 1)
            
            # update
            if (v > read_ratio):
                res = self.crdts[0].addvertex(k, v)
            else:
                res = self.crdts[0].get(k)

            sample_point = self._throughout(start, i, num_ops, sample_point)
            i = i + 1



# if __name__ == "__main__":
#     # kv_pair = generate_kv_pair(1250)
#     # host1 = "127.0.0.1"
#     # port1 = 5000
#     # s = Server(host1, port2)  
#     # s.connect()

#     # host2 = "127.0.0.1"
#     # port2 = 5001
#     # s2 = Server(host1, port2)  
#     # s2.connect()

#     # gcbench = RCounterBench(s, kv_pair)
#     # gcbench.test_set()
#     # gcbench.test_mixed_update_reverse_read(1000000)

#     # s.disconnect()
#     # gctest = GCounterTest({"127.0.0.1": 5000}, 10000)
#     # gctest.init_data()
#     # gctest.test_data()
#     # gctest.test_mixed_update_read(0.5, 100000)
#     # gctest.end()

#     # rgtest = RGraphTest({"127.0.0.1": 5000}, 1000)
#     # rgtest.init_data()
#     # rgtest.test_data()
#     # rgtest.set_reverse(800)
#     # rgtest.test_mixed_update_read(0.5, 100000)
#     # rgtest.end()
#     proc = subprocess.Popen(["dotnet", "run", "-p", "D:\md\Project_RAC\RAC", "D:\md\Project_RAC\RAC\cluster_config.json"], stdout=subprocess.DEVNULL)
#     time.sleep(3)
#     proc2 = subprocess.Popen(["dotnet", "run", "-p", "D:\md\Project_RAC\RAC", "D:\md\Project_RAC\RAC\cluster_config.1.json"], stdout=subprocess.DEVNULL)
#     time.sleep(3)

#     try:

#         kv_pair = generate_kv_pair(1250)
#         host1 = "127.0.0.1"
#         port1 = 5000
#         s = Server(host1, port1)  
#         s.connect()

#         gctest = GCounterTest({"127.0.0.1": 5000}, 10000)
#         gctest.init_data()
#         #gctest.test_data()
#         res = gctest.test_mixed_update_read(0.5, 100000)
#         gctest.end()

#         todraw = []
#         for i in range(len(res)):
#             todraw.append({"x": i, 1:res[i]})

#         print([todraw])

#         write_to_csv("x.csv", ["x", 1], todraw)
#         headers, x, y = read_from_csv("x.csv")
#         print(x)
#         print(y)
#         draw("test", "# of Ops", "Throughput", x, y)

#         s.disconnect()

#     finally:

#         proc.terminate()
#         proc2.terminate()
#         print("end")