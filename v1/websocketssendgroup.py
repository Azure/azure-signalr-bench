import sys
import settings

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
overlap_group_baseStep={1:100, 2:100, 5:300, 10:500, 20:500, 50:3000, 100:3000}
overlap_group_step={1:50, 2:50, 5:100, 10:100, 20:100, 50:1000, 100:1000}
overlap_group_number={1:10000, 2:20000, 5:50000, 10:100000, 20:200000, 50:500000, 100:1000000}
tiny_group_baseStep={1:1000, 2:1000, 5:2000, 10:3000, 20:3000, 50:6000, 100:6000}
tiny_group_step={1:100, 2:200, 5:500, 10:1000, 20:1000, 50:1000, 100:1000}
tiny_group_number={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
small_group_baseStep={1:300, 2:300, 5:500, 10:500, 20:1000, 50:3000, 100:3000}
small_group_step={1:100, 2:100, 5:500, 10:500, 20:500, 50:1000, 100:1000}
small_group_number={1:100, 2:200, 5:500, 10:1000, 20:2000, 50:5000, 100:10000}
big_group_baseStep={1:20, 2:20, 5:10, 10:10, 20:10, 50:10, 100:10}
big_group_step={1:5, 2:5, 5:10, 10:10, 20:10, 50:10, 100:10}
big_group_number={1:10, 2:10, 5:10, 10:10, 20:10, 50:10, 100:10}
if settings.gPerfType == settings.gConstMax:
  websockets_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}
  websockets_concurrentConnection={1:100, 2:100, 5:100, 10:200, 20:200, 50:200, 100:200}
  overlap_group_number={1:15000, 2:30000, 5:75000, 10:150000, 20:300000, 50:750000, 100:1500000}
  tiny_group_number={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}
  small_group_number={1:150, 2:300, 5:750, 10:1500, 20:3000, 50:7500, 100:15000}
  #big_group_number={1:15, 2:15, 5:10, 10:10, 20:10, 50:10, 100:10}

def get_group_step(groupType, index):
   if groupType == "s":
      return small_group_step[index]
   elif groupType == "b":
      return big_group_step[index]
   elif groupType == "t":
      return tiny_group_step[index]
   elif groupType == "o":
      return overlap_group_step[index]
   else:
      return None

def get_group_baseStep(groupType, index):
   if groupType == "s":
      return small_group_baseStep[index]
   elif groupType == "b":
      return big_group_baseStep[index]
   elif groupType == "t":
      return tiny_group_baseStep[index]
   elif groupType == "o":
      return overlap_group_baseStep[index]
   else:
      return None

def get_group_number(groupType, index):
   if groupType == "s":
      return small_group_number[index]
   elif groupType == "b":
      return big_group_number[index]
   elif groupType == "t":
      return tiny_group_number[index]
   elif groupType == "o":
      return overlap_group_number[index]
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
   baseSend=get_group_baseStep(groupType, 1)
   stepSend=get_group_step(groupType, 1)
   groupNum=get_group_number(groupType, 1)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
   baseSend=get_group_baseStep(groupType, 2)
   stepSend=get_group_step(groupType, 2)
   groupNum=get_group_number(groupType, 2)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
   baseSend=get_group_baseStep(groupType, 5)
   stepSend=get_group_step(groupType, 5)
   groupNum=get_group_number(groupType, 5)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
   baseSend=get_group_baseStep(groupType, 10)
   stepSend=get_group_step(groupType, 10)
   groupNum=get_group_number(groupType, 10)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
   baseSend=get_group_baseStep(groupType, 20)
   stepSend=get_group_step(groupType, 20)
   groupNum=get_group_number(groupType, 20)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
   baseSend=get_group_baseStep(groupType, 50)
   stepSend=get_group_step(groupType, 50)
   groupNum=get_group_number(groupType, 50)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
   baseSend=get_group_baseStep(groupType, 100)
   stepSend=get_group_step(groupType, 100)
   groupNum=get_group_number(groupType, 100)
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
groupNum: {groupNum}
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
