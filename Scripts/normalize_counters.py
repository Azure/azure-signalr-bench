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

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the input Yaml file")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      r = normalize(args.input)
      jr = ast.literal_eval(json.dumps(r, sort_keys=True, indent=2))
      nr = str(jr).replace("'", "\"")
      print(nr)
