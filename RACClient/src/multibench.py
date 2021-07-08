import csv
import json
from benchmark import *
from startservers import *
import time

# 1. prep json
# set & variables
# fix each variable, change others
# 2. start servres
# 3. run bench
# 4. collect data
# 5. stop servers

SERVER_LIST = ["192.168.41.205", "192.168.41.188"]


def generate_json(wokload_config: dict, prime_variable, secondary_variable, target_metric, rfilename):

    # y-axis
    primaries = wokload_config[prime_variable]

    # more bars
    secondaries = wokload_config[secondary_variable]

    json_dict = wokload_config.copy()

    result = []

    labels = [prime_variable]
    for s in secondaries:
        labels.append(str(s))

    # running benchmarks
    for p in primaries:

        p_result = {}
        p_result[prime_variable] = p

        for s in secondaries:
            json_dict[prime_variable] = p

            json_dict[secondary_variable] = s

            wlfilename = str(p) + str(s) + ".json"

            if "nodes_pre_server" == primaries:
                num_server = p
            elif "nodes_pre_server" == secondaries:
                num_server = s
            else:
                num_server = json_dict["nodes_pre_server"]

            addresses = start_server_remote(
                num_server, SERVER_LIST[0:wokload_config["use_server"]])

            json_dict["nodes"] = addresses

            with open(wlfilename, 'w') as json_file:
                json.dump(json_dict, json_file)
            time.sleep(2)
            r = run_benchmark(wlfilename)

            if target_metric == 'tp':
                rval = r.tp
            elif target_metric == 'ml':
                rval = np.median(r.get_latency())

            p_result[str(s)] = rval
            stop_server_remote(SERVER_LIST[0:wokload_config["use_server"]])

            os.remove(wlfilename)

            json_dict = wokload_config.copy()

        result.append(p_result)
    print(result)
    parse_result(result, labels, 1, rfilename)


def parse_result(result, labels, target_metric, rfilename):
    with open('results/' + rfilename, 'w') as f:
        writer = csv.DictWriter(f, fieldnames=labels)
        writer.writeheader()
        for elem in result:
            writer.writerow(elem)



def plot():
    pass


if __name__ == "__main__":
    peaktp = {
        "nodes_pre_server": 1,
        "use_server": 2,
        "client_multiplier": 3,

        "typecode": "rc",
        "total_objects": 100,

        "prep_ops_pre_obj": 1000,
        "num_reverse": [0],
        "prep_ratio": [0.5, 0.5, 0],


        "ops_per_object": 1000,
        "op_ratio": [[0.35, 0.35, 0.3]],
        "target_throughput": 0
    }

    generate_json(peaktp, "num_reverse", "op_ratio", "tp", "peak_rc_tp.csv")
