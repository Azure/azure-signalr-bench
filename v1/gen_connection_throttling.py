import argparse
from settings import *
import sys
import yaml

def unit1throttling():
   return 0

def unit2throttling():
   return 0

def unit5throttling():
   return 0

def unit10throttling():
   return 0

def unit20throttling():
   return 20000

def unit50throttling():
   return 20000

def unit100throttling():
   return 20000

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-u", "--unit", help="specify the unit: <unit50>|<unit100>")
   args = parser.parse_args()
   if args.unit != None:
     callfunc="{unit}throttling()".format(unit=args.unit)
     r = eval(callfunc)
     print(r)
