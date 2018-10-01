import sys

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
small_group_number={1:100, 2:200, 5:500, 10:1000, 20:2000, 50:5000, 100:10000}
big_group_number={1:10, 2:10, 5:10, 10:10, 20:10, 50:10, 100:10}

def websocketssendgroupunit1_connection():
   return websockets_connection[1]

def websocketssendgroupunit2_connection():
   return websockets_connection[2]

def websocketssendgroupunit5_connection():
   return websockets_connection[5]

def websocketssendgroupunit10_connection():
   return websockets_connection[10]

def websocketssendgroupunit20_connection():
   return websockets_connection[20]

def websocketssendgroupunit50_connection():
   return websockets_connection[50]

def websocketssendgroupunit100_connection():
   return websockets_connection[100]

def websocketssendgroupunit1_concurrentConnection():
   return websockets_concurrentConnection[1]

def websocketssendgroupunit2_concurrentConnection():
   return websockets_concurrentConnection[2]

def websocketssendgroupunit5_concurrentConnection():
   return websockets_concurrentConnection[5]

def websocketssendgroupunit10_concurrentConnection():
   return websockets_concurrentConnection[10]

def websocketssendgroupunit20_concurrentConnection():
   return websockets_concurrentConnection[20]

def websocketssendgroupunit50_concurrentConnection():
   return websockets_concurrentConnection[50]

def websocketssendgroupunit100_concurrentConnection():
   return websockets_concurrentConnection[100]

def websocketssendgroupunit1(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[1]
   concurrentConnection=websockets_concurrentConnection[1]
   baseSend=20
   stepSend=20
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=(small_group_number[1] if groupType == "s" else (big_group_number[1] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return pipeline

def websocketssendgroupunit2(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=40
   stepSend=20
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=(small_group_number[2] if groupType == "s" else (big_group_number[2] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit3(duration, groupType):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit4(duration, groupType):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))


def websocketssendgroupunit5(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[5]
   concurrentConnection=websockets_concurrentConnection[5]
   baseSend=50
   stepSend=20
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=(small_group_number[5] if groupType == "s" else (big_group_number[5] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return(pipeline)
   

def websocketssendgroupunit6(duration, groupType):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit7(duration, groupType):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit8(duration, groupType):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit9(duration, groupType):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit10(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[10]
   concurrentConnection=websockets_concurrentConnection[10]
   baseSend=10
   stepSend=10
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=(small_group_number[10] if groupType == "s" else (big_group_number[10] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit20(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=10
   stepSend=5
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=(small_group_number[20] if groupType == "s" else (big_group_number[20] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit50(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=5
   stepSend=1
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=(small_group_number[50] if groupType == "s" else (big_group_number[50] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit100(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[100]
   concurrentConnection=websockets_concurrentConnection[100]
   baseSend=1
   stepSend=1
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
overlap: 1
enableGroupJoinLeave: false
pipeline:
- createConn
- startConn
- joinGroup
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,
	groupNum=(small_group_number[100] if groupType == "s" else (big_group_number[100] if groupType == "b" else None)),
	baseSend=baseSend,send=stepSend)
   return(pipeline)
