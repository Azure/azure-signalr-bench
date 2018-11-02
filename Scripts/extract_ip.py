# -*- coding: utf-8 -*-
import argparse
import sys
import yaml

def masterIP(input):
    a='function {func} has not implemented'.format(func=sys._getframe().f_code.co_name)
    return a

def slavesIP(input):
    a='function {func} has not implemented'.format(func=sys._getframe().f_code.co_name)
    return a

def appserverIP(input):
    a='function {func} has not implemented'.format(func=sys._getframe().f_code.co_name)
    return a

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the input Yaml file")
   parser.add_argument("-q", "--query", choices=["master", "slaves", "appserver"], type=str, help="Choose the entity you want to know its IP")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      if args.query != None:
         func="{item}IP".format(item=args.query)
         r = locals()[func](args.input)
         print(r)
