import json
import argparse

def FindMaxItem(infile, queryMax):
    qMax = 0
    jItem = ""
    with open(infile) as f:
       for line in f:
           items = line.rstrip().split(" ")
           if (len(items) == 2):
             try:
                jData = json.loads(items[1], 'utf-8')
                if (queryMax in jData):
                   v = int(jData[queryMax])
                   if (v > qMax):
                     qMax = v
                     jItem = items[1]
             except Exception:
                print("exception occurs")
    print(qMax)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="specify the input json result", required=True)
   parser.add_argument("-q", "--query",
          choices=["clientConnectionCount",
                   "serverConnectionCount",
                   "globalClientConnectionCount",
                   "globalServerConnectionCount",
                   "localRedisPubCount",
                   "globalRedisPubCount",
                   "redisSubCount",
                   "localClientMessageCount",
                   "localServerMessageCount",
                   "clientMessageCount",
                   "serverMessageCount"],
          default='clientConnectionCount',
          type=str, help="specify the query item, default is 'clientConnectionCount'")

   args = parser.parse_args()
   FindMaxItem(args.input, args.query)
