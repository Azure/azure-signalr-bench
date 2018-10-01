import sys

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}

def websocketsechounit1_connection():
   return websockets_connection[1]

def websocketsechounit2_connection():
   return websockets_connection[2]

def websocketsechounit5_connection():
   return websockets_connection[5]

def websocketsechounit10_connection():
   return websockets_connection[10]

def websocketsechounit20_connection():
   return websockets_connection[20]

def websocketsechounit50_connection():
   return websockets_connection[50]

def websocketsechounit100_connection():
   return websockets_connection[100]

def websocketsechounit1_concurrentConnection():
   return websockets_concurrentConnection[1]

def websocketsechounit2_concurrentConnection():
   return websockets_concurrentConnection[2]

def websocketsechounit5_concurrentConnection():
   return websockets_concurrentConnection[5]

def websocketsechounit10_concurrentConnection():
   return websockets_concurrentConnection[10]

def websocketsechounit20_concurrentConnection():
   return websockets_concurrentConnection[20]

def websocketsechounit50_concurrentConnection():
   return websockets_concurrentConnection[50]

def websocketsechounit100_concurrentConnection():
   return websockets_concurrentConnection[100]

def websocketsechounit1(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[1]
   concurrentConnection=websockets_concurrentConnection[1]
   baseSend=websockets_connection[1]
   stepSend=500
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return pipeline

def websocketsechounit2(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=websockets_connection[2]
   stepSend=500
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsechounit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsechounit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))


def websocketsechounit5(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[5]
   concurrentConnection=websockets_concurrentConnection[5]
   baseSend=websockets_connection[5]
   stepSend=1000
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)
   

def websocketsechounit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsechounit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsechounit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsechounit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketsechounit10(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[10]
   concurrentConnection=websockets_concurrentConnection[10]
   baseSend=websockets_connection[10] / 2
   stepSend=websockets_connection[10] / 2
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsechounit20(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=websockets_connection[20] / 4
   stepSend=websockets_connection[20] / 4
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

def websocketsechounit50(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=websockets_connection[50] / 10
   stepSend=websockets_connection[50] / 10
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketsechounit100(duration):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[100]
   concurrentConnection=websockets_concurrentConnection[100]
   baseSend=websockets_connection[100] / 20
   stepSend=websockets_connection[100] / 20
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
