import sys

def gen_normal_steps(connections, send):
  conn=int(connections)
  s=int(send)
  rtn = ""
  if conn % s != 0:
     rtn = "Wrong input for '" + connections + "' and '" + send + "'"
  else:
     step = conn / s
     rtn = ""
     for x in range(step):
        rtn = rtn + "up" + send + ";scenario;"
  return rtn

if __name__=="__main__":
  if (len(sys.argv) != 3):
    print("Input <connections> <send>")
  else:
    print(gen_normal_steps(sys.argv[1], sys.argv[2]))
