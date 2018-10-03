import sys
import settings

serversentevent_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
serversentevent_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
serversentevent_baseStep={1:2, 2:2, 5:2, 10:2, 20:1, 50:1, 100:1}
serversentevent_step={1:2, 2:2, 5:2, 10:2, 20:1, 50:1, 100:1}
if settings.gPerfType == settings.gConstMax:
  serversentevent_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}

def serversenteventsbroadcastunit1_connection():
   return serversentevent_connection[1]

def serversenteventsbroadcastunit2_connection():
   return serversentevent_connection[2]

def serversenteventsbroadcastunit5_connection():
   return serversentevent_connection[5]

def serversenteventsbroadcastunit10_connection():
   return serversentevent_connection[10]

def serversenteventsbroadcastunit20_connection():
   return serversentevent_connection[20]

def serversenteventsbroadcastunit50_connection():
   return serversentevent_connection[50]

def serversenteventsbroadcastunit100_connection():
   return serversentevent_connection[100]

def serversenteventsbroadcastunit1_concurrentConnection():
   return serversentevent_concurrentConnection[1]

def serversenteventsbroadcastunit2_concurrentConnection():
   return serversentevent_concurrentConnection[2]

def serversenteventsbroadcastunit5_concurrentConnection():
   return serversentevent_concurrentConnection[5]

def serversenteventsbroadcastunit10_concurrentConnection():
   return serversentevent_concurrentConnection[10]

def serversenteventsbroadcastunit20_concurrentConnection():
   return serversentevent_concurrentConnection[20]

def serversenteventsbroadcastunit50_concurrentConnection():
   return serversentevent_concurrentConnection[50]

def serversenteventsbroadcastunit100_concurrentConnection():
   return serversentevent_concurrentConnection[100]

def serversenteventsbroadcastunit1(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[1]
   concurrentConnection=serversentevent_concurrentConnection[1]
   baseSend=serversentevent_baseStep[1]
   stepSend=serversentevent_step[1]
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
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsbroadcastunit2(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[2]
   concurrentConnection=serversentevent_concurrentConnection[2]
   baseSend=serversentevent_baseStep[2]
   stepSend=serversentevent_step[2]
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
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsbroadcastunit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsbroadcastunit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsbroadcastunit5(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[5]
   concurrentConnection=serversentevent_concurrentConnection[5]
   baseSend=serversentevent_baseStep[5]
   stepSend=serversentevent_step[5]
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
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsbroadcastunit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsbroadcastunit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsbroadcastunit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsbroadcastunit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsbroadcastunit10(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[10]
   concurrentConnection=serversentevent_concurrentConnection[10]
   baseSend=serversentevent_baseStep[10]
   stepSend=serversentevent_step[10]
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
- up{send}
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

def serversenteventsbroadcastunit20(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[20]
   concurrentConnection=serversentevent_concurrentConnection[20]
   baseSend=serversentevent_baseStep[20]
   stepSend=serversentevent_step[20]
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
- up{send}
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

def serversenteventsbroadcastunit50(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[50]
   concurrentConnection=serversentevent_concurrentConnection[50]
   baseSend=serversentevent_baseStep[50]
   stepSend=serversentevent_step[50]
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
- up{send}
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

def serversenteventsbroadcastunit100(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[100]
   concurrentConnection=serversentevent_concurrentConnection[100]
   baseSend=serversentevent_baseStep[100]
   stepSend=serversentevent_step[100]
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)
