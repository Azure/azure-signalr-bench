import sys
import settings

longpolling_connection={1:500, 2:1000, 5:2500, 10:5000, 20:10000, 50:25000, 100:50000}
longpolling_concurrentConnection={1:100, 2:100, 5:100, 10:100, 20:100, 50:100, 100:200}
longpolling_baseStep={1:50, 2:50, 5:50, 10:50, 20:100, 50:500, 100:500}
longpolling_step={1:50, 2:50, 5:50, 10:50, 20:50, 50:500, 100:500}
if settings.gPerfType == settings.gConstMax:
  longpolling_connection={1:750, 2:1500, 5:3000, 10:7500, 20:15000, 50:30000, 100:75000}

def longpollingechounit1_connection():
   return longpolling_connection[1]

def longpollingechounit2_connection():
   return longpolling_connection[2]

def longpollingechounit5_connection():
   return longpolling_connection[5]

def longpollingechounit10_connection():
   return longpolling_connection[10]

def longpollingechounit20_connection():
   return longpolling_connection[20]

def longpollingechounit50_connection():
   return longpolling_connection[50]

def longpollingechounit100_connection():
   return longpolling_connection[100]

def longpollingechounit1_concurrentConnection():
   return longpolling_concurrentConnection[1]

def longpollingechounit2_concurrentConnection():
   return longpolling_concurrentConnection[2]

def longpollingechounit5_concurrentConnection():
   return longpolling_concurrentConnection[5]

def longpollingechounit10_concurrentConnection():
   return longpolling_concurrentConnection[10]

def longpollingechounit20_concurrentConnection():
   return longpolling_concurrentConnection[20]

def longpollingechounit50_concurrentConnection():
   return longpolling_concurrentConnection[50]

def longpollingechounit100_concurrentConnection():
   return longpolling_concurrentConnection[100]

def longpollingechounit1(duration):
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

def longpollingechounit2(duration):
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

def longpollingechounit3(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingechounit4(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingechounit5(duration):
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

def longpollingechounit6(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingechounit7(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingechounit8(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingechounit9(duration):
   #print sys._getframe().f_code.co_name
   raise ValueError('function {func} has not implemented'.format(func=sys._getframe().f_code.co_name))

def longpollingechounit10(duration):
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

def longpollingechounit20(duration):
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

def longpollingechounit50(duration):
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

def longpollingechounit100(duration):
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
