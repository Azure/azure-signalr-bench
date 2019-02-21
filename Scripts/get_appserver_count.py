import argparse
import sys
import yaml

def handleAppServerCount(unit, query, scneario):
   callfunc="{q}unit.unit{func}_{q}({q}unit.{s})".format(func=unit, q=query, s=scneario)
   r = eval(callfunc)
   print(r)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-u", "--unit", help="specify the unit type: 1|2|5|10|20|50|100")
   parser.add_argument("-q", "--query", choices=["appserver","webappserver"], default="appserver", type=str, help="specify query type: appserver, or webappserver")
   parser.add_argument("-s", "--scenario", choices=["echo", "broadcast", "sendToClient", "sendToGroup", "others"], default="others", type=str, help="specify the scenario, default is other")
   args = parser.parse_args()

   import appserverunit
   import webappserverunit
   if args.unit != None:
      handleAppServerCount(args.unit, args.query, args.scenario)
   else:
      print("Please specify unit")
