# -*- coding: utf-8 -*-
import argparse
import sys
import yaml

def masterIP(input):
    with open(input, 'r') as f:
        data = yaml.load(f)
        return data['masterPrivateIp']

def slavesIP(input):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        slaveList = data['slavePrivateIp'].split(';')
        l = len(slaveList)
        for i, item in enumerate(slaveList):
            ret += item + ":5555"
            if i + 1 < l:
               ret += ","
    return ret

def appserverIP(input):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        appserverList = data['appServerPrivateIp'].split(';')
        l = len(appserverList)
        for i, item in enumerate(appserverList):
            ret += item
            if i + 1< l:
               ret += ","
    return ret

def appserverPubIP(input):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        appserverList = data['appServerPublicIp'].split(';')
        l = len(appserverList)
        for i, item in enumerate(appserverList):
            ret += "http://" + item + ":5050/signalrbench"
            if i + 1< l:
               ret += ","
    return ret

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the input Yaml file")
   parser.add_argument("-q", "--query", choices=["master", "slaves", "appserver", "appserverPub"], type=str, help="Choose the entity you want to know its IP")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      if args.query != None:
         func="{item}IP".format(item=args.query)
         r = locals()[func](args.input)
         print(r)
