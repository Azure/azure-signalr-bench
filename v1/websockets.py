import sys

def websocketsechounit1():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{baseline}
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
- disposeConn'''.format(baseline=1000,send=500)
   print(pipeline)

def websocketsechounit2():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{baseline}
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
- disposeConn'''.format(baseline=2000,send=500)
   print(pipeline)

def websocketsechounit3():
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

def websocketsechounit4():
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

def websocketsechounit5():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{baseline}
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
- disposeConn'''.format(baseline=5000,send=1000)
   print(pipeline)

def websocketsechounit6():
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

def websocketsechounit7():
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

def websocketsechounit8():
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

def websocketsechounit9():
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

def websocketsechounit10():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{baseline}
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
- disposeConn'''.format(baseline=10000,send=2000)
   print(pipeline)

def websocketsechounit20():
   #print sys._getframe().f_code.co_name
   pipeline = '''\
pipeline:
- createConn
- startConn
- up{baseline}
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
- disposeConn'''.format(baseline=20000,send=2000)
   print(pipeline)
