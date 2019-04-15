import argparse
import ast
import json
import sys

def normalize(input):
   data = []
   with open(input) as f:
        for line in f:
            data.append(json.loads(line))
   return data 

def in_memory_normalize(input):
   # may be OOM killed
   r = normalize(args.input)
   jr = ast.literal_eval(json.dumps(r, sort_keys=True, indent=2))
   nr = str(jr).replace("'", "\"")
   print(nr)

def streaming_normalize(input):
   data = []
   # get the total line number
   totalLine = sum(1 for line in open(input))
   if totalLine > 1:
        print("[\n")
   #
   with open(input) as f:
        curLine = 0
        for line in f:
            curLine = curLine + 1
            a=json.loads(line)
            jr = ast.literal_eval(json.dumps(a, sort_keys=True, indent=2))
            nr = str(jr).replace("'", "\"")
            print(nr)
            if curLine < totalLine:
               print(",")
            else:
               print("]")
   return data


if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the input Yaml file")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      #in_memory_normalize(args.input)
      streaming_normalize(args.input)
