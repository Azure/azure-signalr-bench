import sys

def ServerSentEventsEchoUnit1():
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
- disposeConn'''.format(send=80)
   print(pipeline)

def ServerSentEventsEchoUnit2():
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
- disposeConn'''.format(send=80)
   print(pipeline)

def ServerSentEventsEchoUnit3():
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
- disposeConn'''.format(send=80)
   print(pipeline)

def ServerSentEventsEchoUnit4():
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
- disposeConn'''.format(send=80)
   print(pipeline)

def ServerSentEventsEchoUnit5():
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
- disposeConn'''.format(send=80)
   print(pipeline)

def ServerSentEventsEchoUnit6():
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
- stopConn
- disposeConn'''.format(send=160)
   print(pipeline)

def ServerSentEventsEchoUnit7():
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
- stopConn
- disposeConn'''.format(send=160)
   print(pipeline)

def ServerSentEventsEchoUnit8():
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
- stopConn
- disposeConn'''.format(send=160)
   print(pipeline)

def ServerSentEventsEchoUnit9():
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
- stopConn
- disposeConn'''.format(send=160)
   print(pipeline)

def ServerSentEventsEchoUnit10():
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
- stopConn
- disposeConn'''.format(send=160)
   print(pipeline)
