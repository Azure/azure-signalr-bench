import json
import argparse

def Analyze(item):
   sendingStep = item['Counters']['sendingStep']
   received = item['Counters']['message:received']
   successConn = item['Counters']['connection:success']
   errConn = item['Counters']['connection:error']
   totalConn = errConn + successConn
   ge1s = item['Counters']['message:ge:1000']
   ge1sRate = ge1s/float(received)
   errRate = errConn/float(totalConn)
   if (ge1sRate < 0.01 and errRate < 0.01):
      return sendingStep
   return 0
   #return ((1-ge1s/float(received)), (ge1s/float(received)), errConn/float(totalConn))
   #print("%.3f" % (1-ge1s/float(received)))
   #print(sendingStep, "%.3f" % (1-ge1s/float(received)), ("%.3f" % (ge1s/float(received))),
   #     "%.3f" % (errConn/float(totalConn)))

def FindMaxValidSend(jsonFile):
   maxSending = 0
   with open(jsonFile) as f:
       jData = json.load(f, 'utf-8')
       jLen = len(jData)
       for index, item in enumerate(jData):
           if ('sendingStep' in item['Counters']):
               sendingStep = item['Counters']['sendingStep']
               received = item['Counters']['message:received']
               if (sendingStep > 0 and index + 1 < jLen and
                   'sendingStep' in jData[index+1]['Counters'] and
                   sendingStep < jData[index+1]['Counters']['sendingStep'] and
                   received > 0):
                   tmpSend = Analyze(item)
                   if (tmpSend > maxSending):
                      maxSending = sendingStep
       received = item['Counters']['message:received']
       if (received > 0):
          tmpSend = Analyze(item)
          if (tmpSend > maxSending):
             maxSending = sendingStep
       print(maxSending)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="specify the input json result")
   args = parser.parse_args()

   FindMaxValidSend(args.input)
