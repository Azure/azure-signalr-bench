import argparse
from settings import *
import sys
import yaml

def handleAppServerCount(unit):
   callfunc="appserverunit.{func}_appserver()".format(func=unit)
   r = eval(callfunc)
   print(r)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-u", "--unit", help="specify the unit type: <unit1>|<unit2>|<unit5>|<unit10>|<unit20>|<unit50>|<unit100>")
   args = parser.parse_args()

   import appserverunit
   handleAppServerCount(args.unit)
