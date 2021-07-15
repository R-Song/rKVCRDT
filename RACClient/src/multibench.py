#!/usr/bin/python3

import csv
import json
from benchmark import *
from startservers import *
import time
import traceback

# 1. prep json
# set & variables
# fix each variable, change others
# 2. start servres
# 3. run bench
# 4. collect data
# 5. stop servers


SERVER_LIST = ["192.168.41.205", "192.168.41.145"]
#SERVER_LIST = ["192.168.41.136", "192.168.41.145"]
REDO = 0
BUILD_FLAG = True


def generate_json(wokload_config: dict, prime_variable, secondary_variable, rfilename, local = False):

    print("Running: " + rfilename)

    # y-axis
    primaries = wokload_config[prime_variable]

    # more bars
    secondaries = wokload_config[secondary_variable]

    json_dict = wokload_config.copy()

    tp_result = []
    mem_result = []
    latency_results = {}
    

    labels = [prime_variable]
    for s in secondaries:
        labels.append(str(s))

    total = len(primaries) * len(secondaries)
    count = 0

    # running benchmarks
    for p in primaries:

        p_result = {}
        p_result[prime_variable] = p

        pm_result = {}
        pm_result[prime_variable] = p


        for s in secondaries:
            
            redo = REDO
            while True:

                json_dict[prime_variable] = p

                json_dict[secondary_variable] = s

                wlfilename = str(p) + str(s) + ".json"

                if "nodes_pre_server" == primaries:
                    num_server = p
                elif "nodes_pre_server" == secondaries:
                    num_server = s
                else:
                    num_server = json_dict["nodes_pre_server"]

                global BUILD_FLAG

                if local:
                    addresses = start_server(num_server)
                else:
                    addresses = start_server_remote(
                        num_server, SERVER_LIST[0:wokload_config["use_server"]], BUILD_FLAG)

                # only build once per run
                if BUILD_FLAG:
                    BUILD_FLAG = False

                json_dict["nodes"] = addresses

                with open(wlfilename, 'w') as json_file:
                    json.dump(json_dict, json_file)
                time.sleep(5)

                try:
                    r = run_benchmark(wlfilename)

                    redo = 0
                except Exception as e:
                    traceback.print_exc()
                    print("Error, redoing left " + str(redo))

                    if (redo <= 0):
                        print("Error, exiting")
                        parse_tpresult(tp_result, labels, rfilename + "_tp.csv")
                        parse_tpresult(mem_result, labels, rfilename + "_mem.csv")
                        parse_latencyresults(latency_results, rfilename + "_lt.txt")
                        exit()

                finally:
                    if local:
                        stop_server()
                    else:
                        stop_server_remote(SERVER_LIST[0:wokload_config["use_server"]])
                    
                    os.remove(wlfilename)

                    if (redo > 0):
                        redo -= 1
                        continue
                    



                p_result[str(s)] = r.tp
                pm_result[str(s)] = r.mem
                latency_results[str(p) + str(s)] = r.latency_result
                
                
                json_dict = wokload_config.copy()
                count += 1
                print(str(count) + "/" + str(total) + " done")
                time.sleep(5)
                break

        tp_result.append(p_result)
        mem_result.append(pm_result)


    parse_tpresult(tp_result, labels, rfilename + "_tp.csv")
    parse_tpresult(mem_result, labels, rfilename + "_mem.csv")
    parse_latencyresults(latency_results, rfilename + "_lt.txt")

    print("Experiment complete")


def parse_tpresult(result, labels, rfilename):
    with open('results/' + rfilename, 'w') as f:
        writer = csv.DictWriter(f, fieldnames=labels)
        writer.writeheader()
        for elem in result:
            writer.writerow(elem)

def parse_latencyresults(results: dict, rfilename):
    with open('results/' + rfilename, 'w') as f:
        for k, v in results.items():
            f.write("EXP:" + k + "\n")
            for l in v:
                f.write(str(l) + "\n")
            



def plot():
    pass


if __name__ == "__main__":
    test = {
        "nodes_pre_server": 1,
        "use_server": 1,
        "client_multiplier": 10,

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 10,
        "num_reverse": [0],
        "prep_ratio": [1, 0, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.15, 0.15, 0.7]],
        "target_throughput": 0
    }

    test2 = {
        "nodes_pre_server": [5],
        "use_server": 2,
        "client_multiplier": 7,

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": [0],
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 0,
        "op_ratio": [0.25, 0.25, 0.5],
        "target_throughput": 0
    }
    


    #generate_json(test, "num_reverse", "op_ratio", "test", True)
    generate_json(test2, "nodes_pre_server", "num_reverse", "test")



























    peak_tp_check_pnc = {
        "nodes_pre_server": 2,
        "use_server": 2,
        "client_multiplier": [5,6,7,8,9,10],

        "typecode": "pnc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": 0,
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.25, 0.25, 0.5]],
        "target_throughput": 0
    }

    peak_tp_check_rc = {
        "nodes_pre_server": 2,
        "use_server": 2,
        "client_multiplier": [1,2,3,4,5,6,7,8,9,10],

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": 0,
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.25, 0.25, 0.5]],
        "target_throughput": 0
    }

    peak_tp_check_rg = {
        "nodes_pre_server": 1,
        "use_server": 2,
        "client_multiplier": [1,2,3,4,5,6,7],

        "typecode": "rg",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": 0,
        "prep_ratio": [1, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.5, 0.5]],
        "target_throughput": 0
    }

    # ??? to fill
    #generate_json(peak_tp_check_pnc, "client_multiplier", "op_ratio", "peak_tp_check_pnc_lazy_noopt")
    # > 5x clients to fill
    #generate_json(peak_tp_check_rc, "client_multiplier", "op_ratio", "peak_tp_check_rc_lazy_noopt")
    #generate_json(peak_tp_check_rg, "client_multiplier", "op_ratio", "peak_tp_check_rg_lazy_noopt")

    # check peak tp: 
    peak_tp_num_rev_ratio_rc = {
        "nodes_pre_server": 2,
        "use_server": 2,
        "client_multiplier": 7,

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": [0, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 450, 500],
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.15, 0.15, 0.7], [0.25, 0.25, 0.5], [0.35, 0.35, 0.3]],
        "target_throughput": 0
    }

    peak_tp_num_rev_ratio_rg = {
        "nodes_pre_server": 2,
        "use_server": 2,
        "client_multiplier": 7,

        "typecode": "rg",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": [0, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 450, 500],
        "prep_ratio": [1, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.3, 0.7], [0.5, 0.5], [0.7, 0.3]],
        "target_throughput": 0
    }

    # generate_json(peak_tp_num_rev_ratio_rc, "num_reverse", "op_ratio", "peak_tp_num_rev_ratio_rc_lazy_noopt1")
    # generate_json(peak_tp_num_rev_ratio_rc, "num_reverse", "op_ratio", "peak_tp_num_rev_ratio_rc_lazy_noopt2")
    # generate_json(peak_tp_num_rev_ratio_rc, "num_reverse", "op_ratio", "peak_tp_num_rev_ratio_rc_lazy_noopt3")
    # generate_json(peak_tp_num_rev_ratio_rc, "num_reverse", "op_ratio", "peak_tp_num_rev_ratio_rc_lazy_noopt4")
    # generate_json(peak_tp_num_rev_ratio_rc, "num_reverse", "op_ratio", "peak_tp_num_rev_ratio_rc_lazy_noopt5")
    # #generate_json(peak_tp_num_rev_ratio_rg, "num_reverse", "op_ratio", "peak_tp_num_rev_ratio_rg_lazy_noopt")


    # check scalability
    peak_tp_scale_rev_rc = {
        "nodes_pre_server": [1,2,3,4,5,6,7],
        "use_server": 2,
        "client_multiplier": 6,

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": [0, 50, 100, 150, 200, 250, 300],
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 1000,
        "op_ratio": [0.25, 0.25, 0.5],
        "target_throughput": 0
    }
    
    # generate_json(peak_tp_scale_rev_rc, "nodes_pre_server", "num_reverse", "peak_tp_scale_rev_rc_lazy_noopt1")
    # generate_json(peak_tp_scale_rev_rc, "nodes_pre_server", "num_reverse", "peak_tp_scale_rev_rc_lazy_noopt2")
    # generate_json(peak_tp_scale_rev_rc, "nodes_pre_server", "num_reverse", "peak_tp_scale_rev_rc_lazy_noopt3")
    # generate_json(peak_tp_scale_rev_rc, "nodes_pre_server", "num_reverse", "peak_tp_scale_rev_rc_lazy_noopt4")
    # generate_json(peak_tp_scale_rev_rc, "nodes_pre_server", "num_reverse", "peak_tp_scale_rev_rc_lazy_noopt5")


    # check latency
    peak_tp_lt_rev_rc = {
        "nodes_pre_server": 2,
        "use_server": 2,
        "client_multiplier": [1,2,3,4,5,6,7,8,9,10],

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": [0, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500],
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 1000,
        "op_ratio": [0.25, 0.25, 0.5],
        "target_throughput": 0
    }

    #generate_json(peak_tp_lt_rev_rc, "client_multiplier", "num_reverse", "peak_tp_lt_rev_rc_lazy_noopt1")
