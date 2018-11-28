# -*- coding: utf-8 -*-
import argparse
import sys
import yaml

def masterIP(input, count):
    with open(input, 'r') as f:
        data = yaml.load(f)
        return data['masterPrivateIp']

def slaveListIP(input, count):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        slaveList = data['slavePrivateIp'].split(';')
        l = len(slaveList)
        for i, item in enumerate(slaveList):
            ret += item
            if i + 1 < l:
               ret += " "
    return ret

def slavesIP(input, count):
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

def appserverListIP(input, count):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        appserverList = data['appServerPrivateIp'].split(';')
        l = len(appserverList)
        for i, item in enumerate(appserverList):
            if i >= count:
               break
            ret += item
            if i + 1 < l and i + 1 < count:
               ret += " "
    return ret

def appserverIP(input, count):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        appserverList = data['appServerPrivateIp'].split(';')
        l = len(appserverList)
        for i, item in enumerate(appserverList):
            if i >= count:
               break
            ret += item
            if i + 1 < l and i + 1 < count:
               ret += ","
    return ret

def appserverPubIP(input, count):
    ret=""
    with open(input, 'r') as f:
        data = yaml.load(f)
        appserverList = data['appServerPublicIp'].split(';')
        l = len(appserverList)
        for i, item in enumerate(appserverList):
            if i >= count:
               break
            ret += "http://" + item + ":5050/signalrbench"
            if i + 1 < l and i + 1 < count:
               ret += ","
    return ret

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the input Yaml file")
   parser.add_argument("-c", "--count", type=int, default=1000, help="Specify the max server ip count which allows you to get IP list less than original file")
   parser.add_argument("-q", "--query", choices=["master", "slaves", "slaveList", "appserver", "appserverList", "appserverPub"], type=str, help="Choose the entity you want to know its IP")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      if args.query != None:
         func="{item}IP".format(item=args.query)
         r = locals()[func](args.input, args.count)
         print(r)
