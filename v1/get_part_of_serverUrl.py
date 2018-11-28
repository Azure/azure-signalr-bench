# -*- coding: utf-8 -*-
import argparse
import sys
import yaml

def appserverUrl(input, count):
    ret=""
    appUrlList = input.split(';')
    l = len(appUrlList)
    for i, item in enumerate(appUrlList):
        if i >= count:
           break
        ret += item
        if i + 1 < l and i + 1 < count:
           ret += ";"
    return ret

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the url list")
   parser.add_argument("-c", "--count", type=int, default=1000, help="Specify the max server url count")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      r = appserverUrl(args.input, args.count)
      print(r)
