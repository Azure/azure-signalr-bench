import argparse
import sys
import yaml

def handleAppServerCount(unit):
   callfunc="appserverunit.unit{func}_appserver()".format(func=unit)
   r = eval(callfunc)
   print(r)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-u", "--unit", help="specify the unit type: 1|2|5|10|20|50|100")
   args = parser.parse_args()

   import appserverunit
   if args.unit != None:
      handleAppServerCount(args.unit)
   else:
      print("Please specify unit")
