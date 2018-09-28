import sys
import websockets
import longpolling
import serversentevents

myDict = {
  "websocketsechounit1": websockets.WebsocketsEchoUnit1,
  "websocketsechounit2": websockets.WebsocketsEchoUnit2,
  "websocketsechounit3": websockets.WebsocketsEchoUnit3,
  "websocketsechounit4": websockets.WebsocketsEchoUnit4,
  "websocketsechounit5": websockets.WebsocketsEchoUnit5,
  "websocketsechounit6": websockets.WebsocketsEchoUnit6,
  "websocketsechounit7": websockets.WebsocketsEchoUnit7,
  "websocketsechounit8": websockets.WebsocketsEchoUnit8,
  "websocketsechounit9": websockets.WebsocketsEchoUnit9,
  "websocketsechounit10": websockets.WebsocketsEchoUnit10,
  "longpollingechounit1": longpolling.LongpollingEchoUnit1,
  "longpollingechounit2": longpolling.LongpollingEchoUnit2,
  "longpollingechounit3": longpolling.LongpollingEchoUnit3,
  "longpollingechounit4": longpolling.LongpollingEchoUnit4,
  "longpollingechounit5": longpolling.LongpollingEchoUnit5,
  "longpollingechounit6": longpolling.LongpollingEchoUnit6,
  "longpollingechounit7": longpolling.LongpollingEchoUnit7,
  "longpollingechounit8": longpolling.LongpollingEchoUnit8,
  "longpollingechounit9": longpolling.LongpollingEchoUnit9,
  "longpollingechounit10": longpolling.LongpollingEchoUnit10,
  "serversenteventsechounit1": serversentevents.ServerSentEventsEchoUnit1,
  "serversenteventsechounit2": serversentevents.ServerSentEventsEchoUnit2,
  "serversenteventsechounit3": serversentevents.ServerSentEventsEchoUnit3,
  "serversenteventsechounit4": serversentevents.ServerSentEventsEchoUnit4,
  "serversenteventsechounit5": serversentevents.ServerSentEventsEchoUnit5,
  "serversenteventsechounit6": serversentevents.ServerSentEventsEchoUnit6,
  "serversenteventsechounit7": serversentevents.ServerSentEventsEchoUnit7,
  "serversenteventsechounit8": serversentevents.ServerSentEventsEchoUnit8,
  "serversenteventsechounit9": serversentevents.ServerSentEventsEchoUnit9,
  "serversenteventsechounit10": serversentevents.ServerSentEventsEchoUnit10
}

if __name__=="__main__":
   #print(len(sys.argv))
   if (len(sys.argv) != 4):
      print("<transport> <scenario> <unit>")
      exit(0)
   transport=sys.argv[1]
   scenario=sys.argv[2]
   unit=sys.argv[3]
   func = transport.lower()+scenario.lower()+unit.lower()
   #locals()[func]()
   myDict[func]()
