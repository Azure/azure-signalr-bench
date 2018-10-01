import sys

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
group_number={1:100, 2:200, 5:500, 10:1000, 20:2000, 50:5000, 100:10000}

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

def websocketssendgroupunit1(duration):
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
- up{stepSend}
- scenario
- up{stepSend}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=group_number[1],
	baseSend=baseSend,send=stepSend)
   return pipeline

def websocketssendgroupunit2(duration):
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
- up{stepSend}
- scenario
- up{stepSend}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=group_number[2],
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))


def websocketssendgroupunit5(duration):
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
- up{stepSend}
- scenario
- up{stepSend}
- scenario
- leaveGroup
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,groupNum=group_number[5],
	baseSend=baseSend,send=stepSend)
   return(pipeline)
   

def websocketssendgroupunit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendgroupunit10(duration):
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
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit20(duration):
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
pipeline:
- createConn
- startConn
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit50(duration):
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
pipeline:
- createConn
- startConn
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit100(duration):
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
pipeline:
- createConn
- startConn
- up{baseSend}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)
