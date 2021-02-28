import csv
import numpy as np
import matplotlib.pyplot as plt


def write_to_csv(filename, fieldnames, vals):

    csvfile = open(filename, 'w', newline='') 
    writer = csv.DictWriter(csvfile, fieldnames=fieldnames)

    writer.writeheader()
    writer.writerows(vals)

def read_from_csv(filename):
    csvfile = open(filename, 'r', newline='') 
    reader = csv.DictReader(csvfile)
    res = {}
    for row in reader:
        for k, v in row.items():
            k = int(k)
            if k not in res.keys():
                res[k] = []
            
            res[k].append(int(v))

    return res

def draw(name, x_label, y_label, x_values, y_values):
    plt.xlabel(x_label)
    plt.ylabel(y_label)
    plt.title(name)

    t = np.arange(0., 5., 0.2)

    markers = ["-x", "-d", "-s", "-o", "-*", "-+"]

    for i in range(len(y_values)):
        if (i > len(markers)):
            print("TOO MUCH DATA")
            return

        plt.plot(x_values, y_values[i], markers[i])


    plt.show()

if __name__ == "__main__":
    write_to_csv("x.csv", [1, 2], [{1:1, 2:2}, {1:3, 2:4}])
    res = read_from_csv("x.csv")
    print(res)
    x_values = list(res.keys())
    y_values = list(res.values())
    draw("test", "# of Ops", "Throughput", x_values, y_values)