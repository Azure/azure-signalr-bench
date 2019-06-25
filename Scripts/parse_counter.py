import json
import argparse

def Analyze(item):
   if ('connection:connect:success' not in item['Counters']):
      return 0
   sendingStep = item['Counters']['sendingStep']
   received = item['Counters']['message:received']
   successConn = item['Counters']['connection:connect:success']
   errConn = item['Counters']['connection:connect:fail']
   totalConn = errConn + successConn
   ge1s = item['Counters']['message:ge:1000']
   ge1sRate = ge1s/float(received)
   errRate = errConn/float(totalConn)
   if (ge1sRate < 0.01 and errRate < 0.01):
      return sendingStep
   return 0

def IsValid(item):
   received = item['Counters']['message:received']
   successConn = item['Counters']['connection:connect:success']
   errConn = item['Counters']['connection:connect:fail']
   totalConn = errConn + successConn
   ge1s = item['Counters']['message:ge:1000']
   ge1sRate = ge1s/float(received)
   errRate = errConn/float(totalConn)
   if (ge1sRate < 0.01 and errRate < 0.01):
       return 1
   return 0

def GetSendTPuts(cur, next):
   if (IsValid(cur) == 0 or IsValid(next) == 0):
      return 0
   curSendSize = cur['Counters']['message:sentSize']
   nextSendSize = next['Counters']['message:sentSize']
   sendTPuts = 0
   if (nextSendSize > curSendSize):
       sendTPuts = (nextSendSize - curSendSize)
   return sendTPuts

def GetRecvTPuts(cur, next):
   if (IsValid(cur) == 0 or IsValid(next) == 0):
      return 0
   curRecvSize = cur['Counters']['message:recvSize']
   nextRecvSize = next['Counters']['message:recvSize']
   recvTPuts = 0
   if (nextRecvSize > curRecvSize):
       recvTPuts = (nextRecvSize - curRecvSize)
   return recvTPuts

def GetConnection(item):
   if ('connection:connect:success' in item['Counters'] and
       'connection:connect:fail' in item['Counters']):
       successConn = item['Counters']['connection:connect:success']
       errConn = item['Counters']['connection:connect:fail']
       return successConn + errConn
   return 0

def FindMaxValidSend(jsonFile):
   maxSending = 0
   maxConnection = 0
   sendTPuts = 0
   recvTPuts = 0
   curSendSize = 0
   with open(jsonFile) as f:
       jData = json.load(f, 'utf-8')
       jLen = len(jData)
       for index, item in enumerate(jData):
           connection = GetConnection(item)
           if (connection > maxConnection):
               maxConnection = connection
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
               if (sendingStep > 0 and index + 1 < jLen and
                   'sendingStep' in jData[index+1]['Counters'] and
                   sendingStep == jData[index+1]['Counters']['sendingStep'] and
                   'message:received' in jData[index+1]['Counters'] and
                   received > 0 and jData[index+1]['Counters']['message:received'] > 0):
                   if ('message:sentSize' not in jData[index]['Counters']):
                      continue
                   if (jData[index]['Counters']['message:sentSize'] >= curSendSize):
                       curSendSize = jData[index]['Counters']['message:sentSize']
                       stputs = GetSendTPuts(jData[index], jData[index+1])
                       rtputs = GetRecvTPuts(jData[index], jData[index+1])
                       if (stputs > 0 and stputs > sendTPuts):
                           sendTPuts = stputs
                       if (rtputs > 0 and rtputs > recvTPuts):
                           recvTPuts = rtputs
       connection = GetConnection(item)
       if (connection > maxConnection):
           maxConnection = connection
       received = item['Counters']['message:received']
       if (received > 0 and 'sendingStep' in item['Counters']):
          tmpSend = Analyze(item)
          if (tmpSend > maxSending):
             maxSending = sendingStep
       
       print maxConnection,maxSending,sendTPuts,recvTPuts

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="specify the input json result")
   args = parser.parse_args()

   FindMaxValidSend(args.input)
