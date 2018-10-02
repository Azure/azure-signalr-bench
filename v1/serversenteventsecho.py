import sys

serversentevent_connection={1:1000, 2:2000, 5:5000, 10:10000, 20:20000, 50:50000, 100:100000}
serversentevent_concurrentConnection={1:200, 2:200, 5:200, 10:200, 20:200, 50:200, 100:200}

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
   baseSend=100
   stepSend=50
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

def serversenteventsechounit2(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[2]
   concurrentConnection=serversentevent_concurrentConnection[2]
   baseSend=100
   stepSend=50
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
   baseSend=300
   stepSend=50
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
   baseSend=500
   stepSend=50
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

def serversenteventsechounit20(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[20]
   concurrentConnection=serversentevent_concurrentConnection[20]
   baseSend=800
   stepSend=50
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

def serversenteventsechounit50(duration):
   #print sys._getframe().f_code.co_name
   connection=serversentevent_connection[50]
   concurrentConnection=serversentevent_concurrentConnection[50]
   baseSend=1000
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
- up{send}
- scenario
- up{send}
- scenario
- up{send}
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
   baseSend=1000
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
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
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
