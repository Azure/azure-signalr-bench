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
   if (int(received) == 0):
       return 0
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
   if (int(received) == 0):
       return 0
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

def FindMaxSend(index, item, jData, jLen):
    tmpSend = 0
    if ('sendingStep' in item['Counters'] and
        'message:received' in item['Counters']):
        sendingStep = item['Counters']['sendingStep']
        received = item['Counters']['message:received']
        if (sendingStep > 0 and index + 1 < jLen and
            'sendingStep' in jData[index+1]['Counters'] and
            sendingStep < jData[index+1]['Counters']['sendingStep'] and
            received > 0):
            tmpSend = Analyze(item)
        elif (index + 1 == jLen):
            tmpSend = Analyze(item)
    return tmpSend

def FindSendRecvTPuts(index, item, jData, jLen, curSendSize):
    stputs = 0
    rtputs = 0
    if ('sendingStep' in item['Counters'] and
        'message:received' in item['Counters']):
        sendingStep = item['Counters']['sendingStep']
        received = item['Counters']['message:received']
        if (sendingStep > 0 and index + 1 < jLen and
            'sendingStep' in jData[index+1]['Counters'] and
            sendingStep == jData[index+1]['Counters']['sendingStep'] and
            'message:received' in jData[index+1]['Counters'] and
            received > 0 and jData[index+1]['Counters']['message:received'] > 0 and
            'message:sentSize' in jData[index]['Counters'] and
            jData[index]['Counters']['message:sentSize'] >= curSendSize):
            curSendSize = jData[index]['Counters']['message:sentSize']
            stputs = GetSendTPuts(jData[index], jData[index+1])
            rtputs = GetRecvTPuts(jData[index], jData[index+1])
    return stputs, rtputs, curSendSize

def FindMax99ReconnCost(index, item):
    cost = 0
    if ('connection:reconnect:cost:0.99' in item['Counters']):
        cost = item['Counters']['connection:reconnect:cost:0.99']
    return int(cost)

def FindMax99LifeSpan(index, item):
    lifeSpan = 0
    if ('connection:connect:lifespan:0.99' in item['Counters']):
        lifeSpan = item['Counters']['connection:connect:lifespan:0.99']
    return int(lifeSpan)

def FindMaxDropCount(index, item, jData, jLen):
    drop = 0
    if ('sendingStep' in item['Counters'] and
        'message:received' in item['Counters']):
        sendingStep = item['Counters']['sendingStep']
        if (sendingStep > 0 and index + 1 < jLen and
            'sendingStep' in jData[index+1]['Counters'] and
            sendingStep < jData[index+1]['Counters']['sendingStep'] and
            'connection:connect:reconnect' in item['Counters']):
            drop = item['Counters']['connection:connect:reconnect']
        elif (index + 1 == jLen):
            drop = item['Counters']['connection:connect:reconnect']
    return int(drop)

def FindMaxValidSend(jsonFile, requireConnStat):
   maxSending = 0
   maxConnection = 0
   sendTPuts = 0
   recvTPuts = 0
   curSendSize = 0
   drop = 0
   reconnCost = 0
   lifeSpan = 0
   with open(jsonFile) as f:
       jData = json.load(f, 'utf-8')
       jLen = len(jData)
       for index, item in enumerate(jData):
           connection = GetConnection(item)
           if (connection > maxConnection):
               maxConnection = connection
           tmpSend = FindMaxSend(index, item, jData, jLen)
           if (tmpSend > maxSending):
              maxSending = tmpSend
           stputs,rtputs,curSendSize = FindSendRecvTPuts(index, item, jData, jLen, curSendSize)
           if (stputs > sendTPuts):
               sendTPuts = stputs
           if (rtputs > recvTPuts):
               recvTPuts = rtputs
           if (requireConnStat):
               c = FindMax99ReconnCost(index, item)
               if (c > reconnCost):
                   reconnCost = c
               l = FindMax99LifeSpan(index, item)
               if (l > lifeSpan):
                   lifeSpan = l
               d = FindMaxDropCount(index, item, jData, jLen)
               drop = drop + d
       connection = GetConnection(item)
       if (connection > maxConnection):
           maxConnection = connection
       tmpSend = FindMaxSend(index, item, jData, jLen)
       if (tmpSend > maxSending):
           maxSending = tmpSend
       if (requireConnStat):
           drop = drop + FindMaxDropCount(index, item, jData, jLen)
   if (requireConnStat):
       return maxConnection,maxSending,sendTPuts,recvTPuts,drop,reconnCost,lifeSpan
   else:
       return maxConnection,maxSending,sendTPuts,recvTPuts

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", required=True, help="specify the input json result")
   parser.add_argument("-q", "--query", choices=["perf", "longrun"], default="perf", help="specify the query type, default is perf")
   args = parser.parse_args()
   if (args.query == "longrun"):
       maxConnection,maxSending,sendTPuts,recvTPuts,drop,reconnCost,lifeSpan = FindMaxValidSend(args.input, True)
       print maxConnection,maxSending,sendTPuts,recvTPuts,drop,reconnCost,lifeSpan
   else:
       maxConnection,maxSending,sendTPuts,recvTPuts = FindMaxValidSend(args.input, False)
       print maxConnection,maxSending,sendTPuts,recvTPuts
