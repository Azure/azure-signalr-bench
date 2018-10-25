import sys
import settings
## customer's sendgroup scenario

Transport="websockets"
Scenario="SendGroup"
Protocol="json"
Connection=10000
GroupNum=60000
CombineFactor=16
ConcurrentConnection=200
MessageSize="5k"
Send=200
Duration=300
Interval=10
JobConfig='''\
serviceType: ASRSCustomer1
transportType: {transport}
hubProtocol: {protocol}
scenario: {scenario}
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
groupNum: {groupNum}
combineFactor: {combineFactor}
messageSize: {messageSize}
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=Connection, messageSize=MessageSize,
  concurrentConnection=ConcurrentConnection, duration=Duration,
  interval=Interval, groupNum=GroupNum, send=Send, protocol=Protocol,
  transport=Transport, scenario=Scenario, combineFactor=CombineFactor)

def get_info(item):
    return eval(item)

def transport():
    return Transport

def scenario():
    return Scenario

def protocol():
    return Protocol

def jobconfig():
    return JobConfig

def connection():
    return Connection

def concurrentConnection():
    return ConcurrentConnection

def messageSize():
    return MessageSize

def send():
    return Send

def duration():
    return Duration

def interval():
    return Interval

if __name__=="__main__":
    a=get_info('Interval')
    print(a)
