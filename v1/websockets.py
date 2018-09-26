import sys

def WebsocketsEchoUnit1():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(send=500)
   print(pipeline)

def WebsocketsEchoUnit2():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(send=500)
   print(pipeline)

def WebsocketsEchoUnit3():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
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
- disposeConn'''.format(send=500)
   print(pipeline)

def WebsocketsEchoUnit4():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
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
- disposeConn'''.format(send=1000)
   print(pipeline)

def WebsocketsEchoUnit5():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(send=2000)
   print(pipeline)

def WebsocketsEchoUnit6():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(send=2000)
   print(pipeline)

def WebsocketsEchoUnit7():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
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
- disposeConn'''.format(send=2000)
   print(pipeline)

def WebsocketsEchoUnit8():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
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
- disposeConn'''.format(send=2000)
   print(pipeline)

def WebsocketsEchoUnit9():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
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
- disposeConn'''.format(send=4000)
   print(pipeline)

def WebsocketsEchoUnit10():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
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
- disposeConn'''.format(send=4000)
   print(pipeline)
