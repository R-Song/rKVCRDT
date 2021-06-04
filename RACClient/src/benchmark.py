import multiprocessing
from multiprocessing import managers
import string 
import random 
import math
import time
import timeit
import subprocess
from enum import Enum
from client import *
from draw import *
from multiprocessing import Process, Pool


KEY_LEN = 5
STR_LEN = 10

class VAR_TYPE(Enum):
    INT = 1
    STRING = 2

def rand_str(n):
    return ''.join(random.choices(string.ascii_uppercase +
                             string.digits, k = n)) 

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

    def generate_init_req(self):
        res = []
        i = 0
        for key in self.keys:
            res.append(("s", key, i))
            i = i + 1

        return res


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

class Results():
    def __init__(self, num_clients) -> None:
        sharing = multiprocessing.Manager()
        self.tp = []
        self.latency = sharing.list()

    def get_latency(self):
        res = []
        for l in self.latency:
            if l != 0:
                res.append(l / 1000000)

        return res



class TestRunner():
    def __init__(self, nodes, multiplier, data) -> None:
        self.nodes = nodes
        self.num_nodes = len(nodes)
        self.num_clients = math.ceil(self.num_nodes * multiplier)
        self.data = data
        self.connections = self._connect()
        print(self.connections)
        self.crdts = [self.data.CRDT(s) for s in self.connections]
        self.timing = False
        self.results = Results(self.num_clients)

    def _connect(self):
        res = []
        i = 0
        for _ in range(self.num_clients):
            adddress = self.nodes[i]
            ip, port = split_ipport(adddress)
            s = Server(ip, port)
            s.connect()
            res.append(s)

            i += 1
            if i == len(self.nodes):
                i = 0

        return res

    def init_data(self):
        reqs = self.data.generate_init_req()
        c = 0
        for r in reqs:
            res = self.data.op_execute(self.crdts[c], r)
            if not res[0]:
                raise Exception("Initialization failed because " + str(res))
            c += 1
            if (c == len(self.crdts)):
                c = 0



    def split_work(self, list_reqs):
        split = math.ceil(len(list_reqs) / len(self.crdts))
        works = []
        for i in range(0, len(list_reqs), split):
            works.append(list_reqs[i:i + split])

        workers_pool = multiprocessing.Pool(self.num_clients)
        workers_pool.starmap(self.worker, zip(self.crdts, works))
        workers_pool.close()
        workers_pool.join() 

    def worker(self, crdt, list_reqs):
        temp = []
        for req in list_reqs:
            start = time.time_ns() 
            res = self.data.op_execute(crdt, req)
            end = time.time_ns() 
            if (self.timing):
                temp.append(end - start)
        
        for l in temp:
            self.results.latency.append(l)


    def prep_ops(self, total_prep_ops, pre_ops_ratio):
        reqs = self.data.generate_op_values(total_prep_ops, pre_ops_ratio)
        self.split_work(reqs)


    def benchmark(self, total_ops, ops_ratio, throughput = 0):
        '''
        throughput = limit # of ops per second per worker, if 0 then unlimited
        '''
        self.timing = True

        reqs = self.data.generate_op_values(total_ops, ops_ratio)
        start = time.time()
        self.split_work(reqs)
        end = time.time()

        self.results.tp = total_ops / (end - start)
        
        

        
if __name__ == "__main__":
    nodes = ["127.0.0.1:5000", "127.0.0.1:5001"]

    total_objects = 10
    total_ops = 100000
    td = GCExperimentData(total_objects)

    # #td = TestData(RCounter, total_objects, total_ops, [0.25, 0.25, 0.3, 0.2])
    print(td.keys)
    #print(td.generate_op_values(total_ops, [0.5, 0.5]))
    
    print("Starting experiment...")
    tr = TestRunner(nodes, 1, td)

    print("Initializing Data")
    tr.init_data()

    print("Preping Ops")
    tr.prep_ops(20, [1, 0])

    print("Measuing Throughput")
    tr.benchmark(total_ops, [0.5, 0.5])
    
    print(tr.results.tp)
    print( sum(tr.results.get_latency()) / len(tr.results.get_latency()))



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