import sys

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}

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
   baseSend=2
   stepSend=2
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return pipeline

def websocketsbroadcastunit2(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=2
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
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
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
   baseSend=2
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
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
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
   baseSend=2
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
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsbroadcastunit20(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=2
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

def websocketsbroadcastunit50(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=2
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
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsbroadcastunit100(duration):
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
