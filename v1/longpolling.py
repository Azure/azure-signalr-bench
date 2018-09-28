import sys

def LongpollingEchoUnit1():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit2():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit3():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit4():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit5():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit6():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit7():
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
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit8():
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
- up{send}
- scenario
- stopConn
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit9():
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
- up{send}
- scenario
- up{send}
- scenario
- up{send}
- scenario
- stopConn
- disposeConn'''.format(send=50)
   print(pipeline)

def LongpollingEchoUnit10():
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
- disposeConn'''.format(send=50)
   print(pipeline)
