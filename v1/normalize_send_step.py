import sys

def gen_normal_steps(connections, send, expectStep):
  conn=int(connections)
  s=int(send)
  expStep=int(expectStep)
  rtn = ""
  if conn % s != 0:
     rtn = "Wrong input for '" + connections + "' and '" + send + "'"
  else:
     step = conn / s
     if expStep > 0 and expStep < step:
       step = expStep
     rtn = ""
     for x in range(step):
        rtn = rtn + "up" + send + ";scenario;"
  return rtn

if __name__=="__main__":
  if (len(sys.argv) < 3 or len(sys.argv) > 4):
    print("Input <connections> <send> (step)")
    exit(0)
  step=0
  if (len(sys.argv) == 4):
    step = sys.argv[3]
  print(gen_normal_steps(sys.argv[1], sys.argv[2], step))
