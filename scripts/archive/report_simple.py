import sys
import json
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from functools import cmp_to_key


result = sys.argv[1]

with open(result, 'r') as f:
    lines = f.readlines()

# lines parse to json format
statistics = [ json.loads(line) for line in lines if '{' in line and '}' in line]

counters = statistics[-1]['Counters']
received = counters['message:received']

latency = [ (key, counters[key]) for key in counters if 'message:lt' in key or 'message:ge' in key]

def cmp(a, b):
    key1 = a[0]
    key2 = b[0]
    (_, predicate1, latency1) = key1.split(':')
    (_, predicate2, latency2) = key2.split(':')
    
    if predicate1 != predicate2:
        if predicate1 == 'lt': return -1
        return 1
    
    return int(latency1) - int(latency2)

latency.sort(key=cmp_to_key(cmp))


# display result

fig, ax = plt.subplots(figsize=(8, 3), subplot_kw=dict(aspect="equal"))

data = [float(x[1]) for x in latency]
sum_ = sum(data)
keys = ["{x} - {y:.1f}%".format(x=latency[i][0], y=data[i]/sum_*100 if sum_ != 0 else 0) for i in range(0, len(latency))]


wedges, texts, autotexts = ax.pie(data, autopct='%.1f%%', wedgeprops=dict(width=0.618),
                                  textprops=dict(color="black"))

# draw label
bbox_props = dict(boxstyle="square,pad=0.3", fc="w", ec="k", lw=0.72)
kw = dict(xycoords='data', textcoords='data', arrowprops=dict(arrowstyle="-"),
          bbox=bbox_props, zorder=0, va="center")


# draw informations
ax.legend(wedges, keys,
          title="Latency",
          loc="upper left",
          bbox_to_anchor=(-1., 1., 0., 0.))

plt.setp(autotexts, size=8, weight="bold")

ax.set_title("Latency distribution")

plt.savefig('report.svg', format="svg")