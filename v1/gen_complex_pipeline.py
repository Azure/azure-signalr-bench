import argparse
import sys
import websocketsecho
import websocketsbroadcast
import websocketssendgroup
import longpollingecho
import serversenteventsecho

def handleArgs(args):
   transport=args.transport.lower()
   scenario=args.scenario.lower()
   unit=args.unit.lower()
   duration=args.duration
   smallGroup=args.smallGroup
   bigGroup=args.bigGroup
   connections=args.connections
   concurrentConnections=args.concurrentConnections

   func = transport+scenario+unit
   if duration != None:
      # special handle 'sendgroup'
      if scenario == "sendgroup":
         if smallGroup == True:
            callfunc="{transport}.{func}({duration},\"{groupType}\")".format(transport=transport+scenario, func=func, duration=duration, groupType="s")
         elif bigGroup == True:
            callfunc="{transport}.{func}({duration},\"{groupType}\")".format(transport=transport+scenario, func=func, duration=duration, groupType="b")
         else:
            print("You must specify <smallGroup> or <bigGroup>")
            return
      else:
         callfunc="{transport}.{func}({duration})".format(transport=transport+scenario, func=func, duration=duration)
   elif connections != None:
      callfunc="{transport}.{func}_connection()".format(transport=transport+scenario, func=func)
   elif concurrentConnections != None:
      callfunc="{transport}.{func}_concurrentConnection()".format(transport=transport+scenario, func=func)
   #print(callfunc)
   r = eval(callfunc)
   print(r)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-t", "--transport", help="specify the transport type: <websockets>|<serversentevents>|<longpolling>")
   parser.add_argument("-s", "--scenario", help="specify the scenario type: <echo>|<broadcast>|<sendGroup>|<sendToClient>|<sendToFixedClient>|<restSendToUser>|<restBroadcast>")
   parser.add_argument("-u", "--unit", help="specify the unit type: <unit1>|<unit2>|<unit5>|<unit10>|<unit20>|<unit50>|<unit100>")
   stepGroup = parser.add_mutually_exclusive_group()
   stepGroup.add_argument("-d", "--duration", help="specify the duration to run (second)")
   stepGroup.add_argument("-c", "--connections", help="query the connections for given unit", action="store_true")
   stepGroup.add_argument("-C", "--concurrentConnections", help="query the concurrentConnections for given unit", action="store_true")
   sendToGroup = parser.add_mutually_exclusive_group()
   sendToGroup.add_argument("-g", "--smallGroup", help="create small group (10 clients)", action="store_true")
   sendToGroup.add_argument("-G", "--bigGroup", help="create big group (1/10 of all of the whole active clients)", action="store_true")
   args = parser.parse_args()

   if args.transport is None or args.scenario is None or args.unit is None:
      print("transport, scenario, and unit must be specified")
   else:
      handleArgs(args)
