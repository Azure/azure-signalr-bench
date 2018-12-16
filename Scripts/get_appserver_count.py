import argparse
import sys
import yaml

def handleAppServerCount(unit, query):
   callfunc="{q}unit.unit{func}_{q}()".format(func=unit, q=query)
   r = eval(callfunc)
   print(r)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-u", "--unit", help="specify the unit type: 1|2|5|10|20|50|100")
   parser.add_argument("-q", "--query", choices=["appserver","webappserver"], default="appserver", type=str, help="specify query type: appserver, or webappserver")
   args = parser.parse_args()

   import appserverunit
   import webappserverunit
   if args.unit != None:
      handleAppServerCount(args.unit, args.query)
   else:
      print("Please specify unit")
