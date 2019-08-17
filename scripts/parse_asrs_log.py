import json
import argparse
from collections import defaultdict

class CountingWarnRecord:
   def __init__(self, jsonRecord, count):
       self._jsonRecord = jsonRecord
       self._count = count

   def GetCount():
       return self._count

   def GetRecord():
       return self._jsonRecord

   def AddCount(inc):
       self._count += inc

def CategorizedWarns(infile):
   warnsDic = defaultdict(dict)
   warnsCountDic = defaultdict(dict)
   with open(infile) as f:
      for line in f:
         jData = json.loads(line, 'utf-8')
         if (jData['_level'] == "WARN"):
             tmpl = jData['_template']
             warnsCountDic[tmpl] = warnsCountDic.get(tmpl, 0) + 1
             warnsDic[tmpl] = json.dumps(jData)
   return (warnsCountDic,warnsDic)

def details(dic):
    for key in dic.keys():
        out="{key}|{detail}".format(key=key.replace("'", "\\'"),
            detail=dic[key].replace("'", "\\'"))
        print(out)
    
def counter(countDic):
    warnsSum = 0
    for key in countDic.keys():
        warnsSum += countDic[key]
    for key in countDic.keys():
        out="{key}|{percentage:.2f}|{count}".format(key=key.replace("'", "\\'"),
             percentage=countDic[key]/float(warnsSum)*100, count=countDic[key])
        print(out)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="specify the input json result", required=True)
   parser.add_argument("-q", "--query", choices=["details","counter"], help="specify the input json result")
   args = parser.parse_args()
   (counterDic, detailsDic) = CategorizedWarns(args.input)
   if (args.query != None):
      func="{f}({arg}Dic)".format(f=args.query, arg=args.query)
      eval(func)
