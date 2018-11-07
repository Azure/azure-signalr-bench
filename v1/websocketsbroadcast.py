import sys
import settings

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
websockets_baseStep={1:2, 2:2, 5:1, 10:1, 20:1, 50:1, 100:1}
websockets_step={1:2, 2:2, 5:1, 10:1, 20:1, 50:1, 100:1}
if settings.gPerfType == settings.gConstMax:
  websockets_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}

def websocketsbroadcastunit1_connection():
   return websockets_connection[1]

def websocketsbroadcastunit2_connection():
   return websockets_connection[2]

def websocketsbroadcastunit5_connection():
   return websockets_connection[5]

def websocketsbroadcastunit10_connection():
   return websockets_connection[10]

def websocketsbroadcastunit20_connection():
   return websockets_connection[20]

def websocketsbroadcastunit50_connection():
   return websockets_connection[50]

def websocketsbroadcastunit100_connection():
   return websockets_connection[100]

def websocketsbroadcastunit1_concurrentConnection():
   return websockets_concurrentConnection[1]

def websocketsbroadcastunit2_concurrentConnection():
   return websockets_concurrentConnection[2]

def websocketsbroadcastunit5_concurrentConnection():
   return websockets_concurrentConnection[5]

def websocketsbroadcastunit10_concurrentConnection():
   return websockets_concurrentConnection[10]

def websocketsbroadcastunit20_concurrentConnection():
   return websockets_concurrentConnection[20]

def websocketsbroadcastunit50_concurrentConnection():
   return websockets_concurrentConnection[50]

def websocketsbroadcastunit100_concurrentConnection():
   return websockets_concurrentConnection[100]

def websocketsbroadcastunit1(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[1]
   concurrentConnection=websockets_concurrentConnection[1]
   baseSend=websockets_baseStep[1]
   stepSend=websockets_step[1]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend,interval=interval)
   return pipeline

def websocketsbroadcastunit2(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=websockets_baseStep[2]
   stepSend=websockets_step[2]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend,interval=interval)
   return(pipeline)

def websocketsbroadcastunit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsbroadcastunit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))


def websocketsbroadcastunit5(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[5]
   concurrentConnection=websockets_concurrentConnection[5]
   baseSend=websockets_baseStep[5]
   stepSend=websockets_step[5]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend, interval=interval)
   return(pipeline)
   

def websocketsbroadcastunit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsbroadcastunit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsbroadcastunit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsbroadcastunit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsbroadcastunit10(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[10]
   concurrentConnection=websockets_concurrentConnection[10]
   baseSend=websockets_baseStep[10]
   stepSend=websockets_step[10]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
	duration=duration,baseSend=baseSend,send=stepSend,interval=interval)
   return(pipeline)

def websocketsbroadcastunit20(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=websockets_baseStep[20]
   stepSend=websockets_step[20]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend,interval=interval)
   return(pipeline)

def websocketsbroadcastunit50(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=websockets_baseStep[50]
   stepSend=websockets_step[50]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
	duration=duration,baseSend=baseSend,send=stepSend,interval=interval)
   return(pipeline)

def websocketsbroadcastunit100(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[100]
   concurrentConnection=websockets_concurrentConnection[100]
   baseSend=websockets_baseStep[100]
   stepSend=websockets_step[100]
   interval=settings.gInterval
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: {interval}
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
	duration=duration,baseSend=baseSend,send=stepSend,interval=interval)
   return(pipeline)
