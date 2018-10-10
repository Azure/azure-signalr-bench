import sys
import settings

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
websockets_baseStep={1:100, 2:200, 5:1000, 10:1000, 20:1000, 50:5000, 100:5000}
websockets_step={1:100, 2:100, 5:100, 10:1000, 20:1000, 50:1000, 100:1000}
tiny_group_number={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
small_group_number={1:100, 2:200, 5:500, 10:1000, 20:2000, 50:5000, 100:10000}
big_group_number={1:10, 2:10, 5:10, 10:10, 20:10, 50:10, 100:10}
if settings.gPerfType == settings.gConstMax:
  websockets_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}
  websockets_concurrentConnection={1:100, 2:100, 5:100, 10:200, 20:200, 50:200, 100:200}
  websockets_baseStep={1:100, 2:200, 5:1000, 10:1000, 20:1000, 50:5000, 100:5000}
  websockets_step={1:100, 2:100, 5:100, 10:1000, 20:1000, 50:1000, 100:1000}
  tiny_group_number={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}
  small_group_number={1:150, 2:300, 5:750, 10:1500, 20:3000, 50:7500, 100:15000}
  big_group_number={1:10, 2:10, 5:10, 10:10, 20:10, 50:10, 100:10}

def get_group_number(groupType, index):
   if groupType == "s":
      return small_group_number[index]
   elif groupType == "b":
      return big_group_number[index]
   elif groupType == "t":
      return tiny_group_number[index]
   else:
      return None

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
   baseSend=websockets_baseStep[1]
   stepSend=websockets_step[1]
   groupNum=get_group_number(groupType, 1)
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
- up{send}
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
	duration=duration,groupNum=groupNum,
	baseSend=baseSend,send=stepSend)
   return pipeline

def websocketssendgroupunit2(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=websockets_baseStep[2]
   stepSend=websockets_step[2]
   groupNum=get_group_number(groupType, 2)
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
- up{send}
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
	duration=duration,groupNum=groupNum,
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
   baseSend=websockets_baseStep[5]
   stepSend=websockets_step[5]
   groupNum=get_group_number(groupType, 5)
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
- up{send}
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
	duration=duration,groupNum=groupNum,
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
   baseSend=websockets_baseStep[10]
   stepSend=websockets_step[10]
   groupNum=get_group_number(groupType, 10)
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
- up{send}
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
	duration=duration,groupNum=groupNum,
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit20(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=websockets_baseStep[20]
   stepSend=websockets_step[20]
   groupNum=get_group_number(groupType, 20)
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
	duration=duration,groupNum=groupNum,
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit50(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=websockets_baseStep[50]
   stepSend=websockets_step[50]
   groupNum=get_group_number(groupType, 50)
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
	duration=duration,groupNum=groupNum,
	baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendgroupunit100(duration, groupType):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[100]
   concurrentConnection=websockets_concurrentConnection[100]
   baseSend=websockets_baseStep[100]
   stepSend=websockets_step[100]
   groupNum=get_group_number(groupType, 100)
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
	groupNum=groupNum,
	baseSend=baseSend,send=stepSend)
   return(pipeline)
