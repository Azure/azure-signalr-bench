import sys
import websocketsecho
import websocketsbroadcast
import longpollingecho
import serversenteventsecho

if __name__=="__main__":
   #print(len(sys.argv))
   if (len(sys.argv) != 5 and len(sys.argv) != 4):
      print("<transport> <scenario> <unit> <duration> or")
      print("<transport> <scenario> <unit>")
      exit(0)
   transport=sys.argv[1].lower()
   scenario=sys.argv[2].lower()
   unit=sys.argv[3].lower()
   func = transport+scenario+unit
   if (len(sys.argv) == 5):
      duration=sys.argv[4]
      callfunc="{transport}.{func}({duration})".format(transport=transport+scenario, func=func, duration=duration)
   elif (len(sys.argv) == 4):
      callfunc="{transport}.{func}_connection()".format(transport=transport+scenario, func=func)
   r = eval(callfunc)
   print(r)
