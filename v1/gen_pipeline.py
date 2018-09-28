import sys
import websockets
import longpolling
import serversentevents


if __name__=="__main__":
   #print(len(sys.argv))
   if (len(sys.argv) != 4):
      print("<transport> <scenario> <unit>")
      exit(0)
   transport=sys.argv[1]
   scenario=sys.argv[2]
   unit=sys.argv[3]
   func = transport.lower()+scenario.lower()+unit.lower()
   realFunc = eval(transport.lower()+"."+func)
   realFunc()
   #locals()[func]()
