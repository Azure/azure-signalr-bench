import sys
import settings

serversentevent_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
serversentevent_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
serversentevent_baseStep={1:50, 2:50, 5:300, 10:500, 20:700, 50:2000, 100:5000}
serversentevent_step={1:50, 2:50, 5:50, 10:50, 20:50, 50:100, 100:1000}
if settings.gPerfType == settings.gConstMax:
  serversentevent_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}

def serversenteventsechounit1_connection():
   return serversentevent_connection[1]

def serversenteventsechounit2_connection():
   return serversentevent_connection[2]

def serversenteventsechounit5_connection():
   return serversentevent_connection[5]

def serversenteventsechounit10_connection():
   return serversentevent_connection[10]

def serversenteventsechounit20_connection():
   return serversentevent_connection[20]

def serversenteventsechounit50_connection():
   return serversentevent_connection[50]

def serversenteventsechounit100_connection():
   return serversentevent_connection[100]

def serversenteventsechounit1_concurrentConnection():
   return serversentevent_concurrentConnection[1]

def serversenteventsechounit2_concurrentConnection():
   return serversentevent_concurrentConnection[2]

def serversenteventsechounit5_concurrentConnection():
   return serversentevent_concurrentConnection[5]

def serversenteventsechounit10_concurrentConnection():
   return serversentevent_concurrentConnection[10]

def serversenteventsechounit20_concurrentConnection():
   return serversentevent_concurrentConnection[20]

def serversenteventsechounit50_concurrentConnection():
   return serversentevent_concurrentConnection[50]

def serversenteventsechounit100_concurrentConnection():
   return serversentevent_concurrentConnection[100]

def serversenteventsechounit1(duration):
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsechounit2(duration):
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsechounit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsechounit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsechounit5(duration):
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsechounit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsechounit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsechounit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsechounit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def serversenteventsechounit10(duration):
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsechounit20(duration):
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def serversenteventsechounit50(duration):
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

def serversenteventsechounit100(duration):
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
