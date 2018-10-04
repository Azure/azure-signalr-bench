import sys
import settings

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
websockets_baseStep={1:2, 2:2, 5:2, 10:2, 20:1, 50:1, 100:1}
websockets_step={1:2, 2:2, 5:2, 10:2, 20:1, 50:1, 100:1}
if settings.gPerfType == settings.gConstMax:
  websockets_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}

def websocketsrestbroadcastunit1_connection():
   return websockets_connection[1]

def websocketsrestbroadcastunit2_connection():
   return websockets_connection[2]

def websocketsrestbroadcastunit5_connection():
   return websockets_connection[5]

def websocketsrestbroadcastunit10_connection():
   return websockets_connection[10]

def websocketsrestbroadcastunit20_connection():
   return websockets_connection[20]

def websocketsrestbroadcastunit50_connection():
   return websockets_connection[50]

def websocketsrestbroadcastunit100_connection():
   return websockets_connection[100]

def websocketsrestbroadcastunit1_concurrentConnection():
   return websockets_concurrentConnection[1]

def websocketsrestbroadcastunit2_concurrentConnection():
   return websockets_concurrentConnection[2]

def websocketsrestbroadcastunit5_concurrentConnection():
   return websockets_concurrentConnection[5]

def websocketsrestbroadcastunit10_concurrentConnection():
   return websockets_concurrentConnection[10]

def websocketsrestbroadcastunit20_concurrentConnection():
   return websockets_concurrentConnection[20]

def websocketsrestbroadcastunit50_concurrentConnection():
   return websockets_concurrentConnection[50]

def websocketsrestbroadcastunit100_concurrentConnection():
   return websockets_concurrentConnection[100]

def websocketsrestbroadcastunit1(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[1]
   concurrentConnection=websockets_concurrentConnection[1]
   baseSend=websockets_baseStep[1]
   stepSend=websockets_step[1]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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
	duration=duration,baseSend=baseSend,send=stepSend)
   return pipeline

def websocketsrestbroadcastunit2(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=websockets_baseStep[2]
   stepSend=websockets_step[2]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsrestbroadcastunit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsrestbroadcastunit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))


def websocketsrestbroadcastunit5(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[5]
   concurrentConnection=websockets_concurrentConnection[5]
   baseSend=websockets_baseStep[5]
   stepSend=websockets_step[5]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)
   

def websocketsrestbroadcastunit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsrestbroadcastunit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsrestbroadcastunit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsrestbroadcastunit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsrestbroadcastunit10(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[10]
   concurrentConnection=websockets_concurrentConnection[10]
   baseSend=websockets_baseStep[10]
   stepSend=websockets_step[10]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsrestbroadcastunit20(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=websockets_baseStep[20]
   stepSend=websockets_step[20]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsrestbroadcastunit50(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=websockets_baseStep[50]
   stepSend=websockets_step[50]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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

def websocketsrestbroadcastunit100(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[100]
   concurrentConnection=websockets_concurrentConnection[100]
   baseSend=websockets_baseStep[100]
   stepSend=websockets_step[100]
   pipeline = '''\
connection: {connection}
concurrentConnection: {concurrentConnection}
duration: {duration}
interval: 1
pipeline:
- createRestClientConn
- startRestClientConn
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
