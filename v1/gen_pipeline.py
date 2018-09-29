import sys
import websockets
import longpolling
import serversentevents

if __name__=="__main__":
   #print(len(sys.argv))
   if (len(sys.argv) != 5):
      print("<transport> <scenario> <unit> <duration>")
      exit(0)
   transport=sys.argv[1]
   scenario=sys.argv[2]
   unit=sys.argv[3]
   duration=sys.argv[4]
   func = transport+scenario+unit
   #locals()[func]()
   callfunc="{transport}.{func}({duration})".format(transport=transport, func=func, duration=duration)
   r = eval(callfunc)
   print(r)
