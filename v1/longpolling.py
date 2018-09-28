import sys

def longpollingechounit1():
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

def longpollingechounit2():
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

def longpollingechounit3():
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

def longpollingechounit4():
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

def longpollingechounit5():
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

def longpollingechounit6():
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

def longpollingechounit7():
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

def longpollingechounit8():
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

def longpollingechounit9():
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

def longpollingechounit10():
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
