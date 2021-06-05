import multiprocessing
from multiprocessing import Manager, managers
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

def mix_lists(lists):
    res = []

    for i in range(len(lists[0])):
        for l in lists:
            res.append(l[i])

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


class ExperimentData():
    def __init__(self, num_objects, keys = []) -> None:
        self.num_objects = num_objects
        self.keys = keys if len(keys) > 0 else self._generate_keys()

    def CRDT(self, server):
        raise NotImplementedError

    def generate_init_req(self):
        raise NotImplementedError
        
    def generate_op_values(self, num_ops, ops_ratio, reverse=0):
        raise NotImplementedError

    def op_execute(self, crdt, req, last_res=""):
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


    def generate_op_values(self, num_ops, ops_ratio, reverse=0):
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

    def op_execute(self, crdt, req, last_res=""):
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

class RCExperimentData(GCExperimentData):
    def CRDT(self, server):
        return RCounter(server)

    def generate_op_values(self, num_ops, ops_ratio, reverse=0):
        '''
        reverse: num of reverse each key has
        '''
        res = []

        for k in self.keys:
            
            reqs = []
            values = self._generate_values(num_ops, VAR_TYPE.INT)
            ops = self._generate_ops(num_ops, ops_ratio, ["i", "d", "g"])
            assert len(ops) == len(values)

            # reverse interval 
            if reverse > 0:
                r_interval = math.ceil(num_ops / (reverse + 1))


            r_cnt = 0
            for i in range(len(values)):
                v = values[i]
                op = ops[i]
                reqs.append((op, k, v))


                if (reverse > 0 and r_cnt < reverse and (i + 1)  % r_interval == 0):
                    reqs.append(("r", k, ""))
                    r_cnt += 1

            if (reverse > 0 and r_cnt < reverse):
                reqs.append(("r", k, ""))

            res.append(reqs)

        return res

    def op_execute(self, crdt, req, last_res=""):
        op = req[0]
        key = req[1]
        v = req[2]

        if op == "g":
            res = crdt.get(key)
        elif op == "s":
            res = crdt.set(key, v)
        elif op == "i":
            res = crdt.inc(key, v)
        elif op == "d":
            res = crdt.dec(key, v)
        elif op == "r":
            res = crdt.rev(key, last_res)
        else:
            raise ValueError("Incorrect input req: " + str(req))

        return res


class TestRunner():
    
    def __init__(self, nodes, multiplier, data, SharedManager) -> None:
        self.nodes = nodes
        self.num_nodes = len(nodes)
        self.num_clients = math.ceil(self.num_nodes * multiplier)
        self.data = data
        self.connections = self._connect()
        self.crdts = [self.data.CRDT(s) for s in self.connections]
        self.timing = False
        self.do_reverse = False
        self.results = Results(self.num_clients)
        self.rid = SharedManager.dict()
        for k in self.data.keys:
            self.rid[k] = ""
        

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
            works.append(mix_lists(list_reqs[i:i + split]))

        workers_pool = multiprocessing.Pool(self.num_clients)
        workers_pool.starmap(self.worker, zip(self.crdts, works))
        workers_pool.close()
        workers_pool.join() 

    def worker(self, crdt, list_reqs):
        temp = []
        last_rid = {}
        for req in list_reqs:
            start = time.time_ns() 
            
            if self.do_reverse and req[0] == "r":
                try:
                    res = self.data.op_execute(crdt, req, last_rid[req[1]])
                    last_rid[req[1]] = res[1][0]
                except Exception:
                    continue
            else:
                res = self.data.op_execute(crdt, req)

            end = time.time_ns() 
            if (self.timing):
                temp.append(end - start)
        
        for l in temp:
            self.results.latency.append(l)


    def prep_ops(self, total_prep_ops, pre_ops_ratio, reverse=0):
        if reverse > 0: 
            self.do_reverse = True 
        reqs = self.data.generate_op_values(total_prep_ops, pre_ops_ratio, reverse)
        self.split_work(reqs)


    def benchmark(self, ops_per_object, ops_ratio, throughput = 0):
        '''
        throughput = limit # of ops per second per worker, if 0 then unlimited
        '''
        self.do_reverse = False
        self.timing = True

        reqs = self.data.generate_op_values(ops_per_object, ops_ratio)
        start = time.time()
        self.split_work(reqs)
        end = time.time()

        self.results.tp = (ops_per_object * len(self.data.keys)) / (end - start)
        
        

        
if __name__ == "__main__":
    manager = multiprocessing.Manager()
    nodes = ["127.0.0.1:5000", "127.0.0.1:5001"]

    total_objects = 100

    prep_ops_pre_obj = 1000
    num_reverse = 100
    prep_ratio = [0.5, 0.5, 0]

    ops_per_object = 1000
    op_ratio = [0.25, 0.25, 0.5]

    #td = GCExperimentData(total_objects)   
    td = RCExperimentData(total_objects)
    print(td.keys)

    print("Starting experiment...")
    tr = TestRunner(nodes, 1, td, manager)

    print("Initializing Data")
    tr.init_data()

    print("Preping Ops")
    tr.prep_ops(prep_ops_pre_obj, prep_ratio, num_reverse)

    print("Total ops:" + str(total_objects * ops_per_object))
    print("Measuing Throughput")
    tr.benchmark(ops_per_object, op_ratio)
    
    print(tr.results.tp)
    print( sum(tr.results.get_latency()) / len(tr.results.get_latency()))


