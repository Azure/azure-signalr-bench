import sys
import settings

longpolling_connection={1:500, 2:1000, 5:2500, 10:5000, 20:10000, 50:25000, 100:50000}
longpolling_concurrentConnection={1:100, 2:100, 5:100, 10:100, 20:100, 50:100, 100:200}
longpolling_baseStep={1:1, 2:1, 5:1, 10:1, 20:1, 50:1, 100:1}
longpolling_step={1:1, 2:1, 5:1, 10:1, 20:1, 50:1, 100:1}
if settings.gPerfType == settings.gConstMax:
  longpolling_connection={1:750, 2:1500, 5:3000, 10:7500, 20:15000, 50:30000, 100:75000}
  longpolling_concurrentConnection={1:100, 2:100, 5:100, 10:100, 20:100, 50:100, 100:200}

def longpollingbroadcastunit1_connection():
   return longpolling_connection[1]

def longpollingbroadcastunit2_connection():
   return longpolling_connection[2]

def longpollingbroadcastunit5_connection():
   return longpolling_connection[5]

def longpollingbroadcastunit10_connection():
   return longpolling_connection[10]

def longpollingbroadcastunit20_connection():
   return longpolling_connection[20]

def longpollingbroadcastunit50_connection():
   return longpolling_connection[50]

def longpollingbroadcastunit100_connection():
   return longpolling_connection[100]

def longpollingbroadcastunit1_concurrentConnection():
   return longpolling_concurrentConnection[1]

def longpollingbroadcastunit2_concurrentConnection():
   return longpolling_concurrentConnection[2]

def longpollingbroadcastunit5_concurrentConnection():
   return longpolling_concurrentConnection[5]

def longpollingbroadcastunit10_concurrentConnection():
   return longpolling_concurrentConnection[10]

def longpollingbroadcastunit20_concurrentConnection():
   return longpolling_concurrentConnection[20]

def longpollingbroadcastunit50_concurrentConnection():
   return longpolling_concurrentConnection[50]

def longpollingbroadcastunit100_concurrentConnection():
   return longpolling_concurrentConnection[100]

def longpollingbroadcastunit1(duration):
   #print sys._getframe().f_code.co_name
   connection=longpolling_connection[1]
   concurrentConnection=longpolling_concurrentConnection[1]
   baseSend=longpolling_baseStep[1]
   stepSend=longpolling_step[1]
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

def longpollingbroadcastunit2(duration):
   #print sys._getframe().f_code.co_name
   connection=longpolling_connection[2]
   concurrentConnection=longpolling_concurrentConnection[2]
   baseSend=longpolling_baseStep[2]
   stepSend=longpolling_step[2]
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

def longpollingbroadcastunit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingbroadcastunit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingbroadcastunit5(duration):
   connection=longpolling_connection[5]
   concurrentConnection=longpolling_concurrentConnection[5]
   baseSend=longpolling_baseStep[5]
   stepSend=longpolling_step[5]
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

def longpollingbroadcastunit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingbroadcastunit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingbroadcastunit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingbroadcastunit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingbroadcastunit10(duration):
   #print sys._getframe().f_code.co_name
   connection=longpolling_connection[10]
   concurrentConnection=longpolling_concurrentConnection[10]
   baseSend=longpolling_baseStep[10]
   stepSend=longpolling_step[10]
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

def longpollingbroadcastunit20(duration):
   #print sys._getframe().f_code.co_name
   connection=longpolling_connection[20]
   concurrentConnection=longpolling_concurrentConnection[20]
   baseSend=longpolling_baseStep[20]
   stepSend=longpolling_step[20]
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

def longpollingbroadcastunit50(duration):
   #print sys._getframe().f_code.co_name
   connection=longpolling_connection[50]
   concurrentConnection=longpolling_concurrentConnection[50]
   baseSend=longpolling_baseStep[50]
   stepSend=longpolling_step[50]
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

def longpollingbroadcastunit100(duration):
   #print sys._getframe().f_code.co_name
   connection=longpolling_connection[100]
   concurrentConnection=longpolling_concurrentConnection[100]
   baseSend=longpolling_baseStep[100]
   stepSend=longpolling_step[100]
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
