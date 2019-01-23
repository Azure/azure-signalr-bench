import sys
import settings

websockets_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
websockets_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}
websockets_baseStep_256={1:2000, 2:2000, 5:5000, 10:10000, 20:10000, 50:10000, 100:10000}
websockets_step_256={1:200, 2:200, 5:1000, 10:1000, 20:1000, 50:1000, 100:1000}
websockets_baseStep_2k={1:1200, 2:1200, 5:3000, 10:5000, 20:5000, 50:5000, 100:5000}
websockets_step_2k={1:100, 2:100, 5:1000, 10:1000, 20:1000, 50:2000, 100:2000}
websockets_baseStep_16k={1:100, 2:100, 5:500, 10:1000, 20:1000, 50:1000, 100:1000}
websockets_step_16k={1:100, 2:100, 5:200, 10:1000, 20:1000, 50:1000, 100:1000}
websockets_baseStep_128k={1:5, 2:5, 5:5, 10:30, 20:100, 50:200, 100:500}
websockets_step_128k={1:5, 2:5, 5:5, 10:10, 20:50, 50:100, 100:100}
if settings.gPerfType == settings.gConstMax:
  websockets_connection={1:1500, 2:3000, 5:7500, 10:15000, 20:30000, 50:75000, 100:150000}
  websockets_concurrentConnection={1:100, 2:100, 5:100, 10:200, 20:200, 50:200, 100:200}

def get_baseStep(sz, index):
   k = "websockets_baseStep_{sz}".format(sz=sz)
   return globals()[k][index]

def get_step(sz, index):
   k = "websockets_step_{sz}".format(sz=sz)
   return globals()[k][index]

def websocketssendtoclientunit1_connection():
   return websockets_connection[1]

def websocketssendtoclientunit2_connection():
   return websockets_connection[2]

def websocketssendtoclientunit5_connection():
   return websockets_connection[5]

def websocketssendtoclientunit10_connection():
   return websockets_connection[10]

def websocketssendtoclientunit20_connection():
   return websockets_connection[20]

def websocketssendtoclientunit50_connection():
   return websockets_connection[50]

def websocketssendtoclientunit100_connection():
   return websockets_connection[100]

def websocketssendtoclientunit1_concurrentConnection():
   return websockets_concurrentConnection[1]

def websocketssendtoclientunit2_concurrentConnection():
   return websockets_concurrentConnection[2]

def websocketssendtoclientunit5_concurrentConnection():
   return websockets_concurrentConnection[5]

def websocketssendtoclientunit10_concurrentConnection():
   return websockets_concurrentConnection[10]

def websocketssendtoclientunit20_concurrentConnection():
   return websockets_concurrentConnection[20]

def websocketssendtoclientunit50_concurrentConnection():
   return websockets_concurrentConnection[50]

def websocketssendtoclientunit100_concurrentConnection():
   return websockets_concurrentConnection[100]

def websocketssendtoclientunit1(duration, sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[1]
   concurrentConnection=websockets_concurrentConnection[1]
   baseSend=get_baseStep(sz, 1)
   stepSend=get_step(sz, 1)
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
- disposeConn'''.format(connection=connection, concurrentConnection=concurrentConnection,
          duration=duration,baseSend=baseSend,send=stepSend)
   return pipeline

def websocketssendtoclientunit2(duration, sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[2]
   concurrentConnection=websockets_concurrentConnection[2]
   baseSend=get_baseStep(sz, 2)
   stepSend=get_step(sz, 2)
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendtoclientunit3(duration,sz):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendtoclientunit4(duration,sz):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))


def websocketssendtoclientunit5(duration,sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[5]
   concurrentConnection=websockets_concurrentConnection[5]
   baseSend=get_baseStep(sz, 5)
   stepSend=get_step(sz, 5)
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)
   

def websocketssendtoclientunit6(duration,sz):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendtoclientunit7(duration,sz):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendtoclientunit8(duration,sz):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendtoclientunit9(duration,sz):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def websocketssendtoclientunit10(duration,sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[10]
   concurrentConnection=websockets_concurrentConnection[10]
   baseSend=get_baseStep(sz, 10)
   stepSend=get_step(sz, 10)
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
- stopConn
- disposeConn'''.format(connection=connection,
	concurrentConnection=concurrentConnection,
	duration=duration,baseSend=baseSend,send=stepSend)
   return(pipeline)

def websocketssendtoclientunit20(duration,sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[20]
   concurrentConnection=websockets_concurrentConnection[20]
   baseSend=get_baseStep(sz, 20)
   stepSend=get_step(sz, 20)
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

def websocketssendtoclientunit50(duration,sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[50]
   concurrentConnection=websockets_concurrentConnection[50]
   baseSend=get_baseStep(sz, 50)
   stepSend=get_step(sz, 50)
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

def websocketssendtoclientunit100(duration,sz):
   #print sys._getframe().f_code.co_name
   connection=websockets_connection[100]
   concurrentConnection=websockets_concurrentConnection[100]
   baseSend=get_baseStep(sz, 100)
   stepSend=get_step(sz, 100)
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
