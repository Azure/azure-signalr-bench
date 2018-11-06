
# -*- coding: utf-8 -*-
import argparse
import sys
import yaml

def replaceHubUrl(yamlData, hubUrls):
   pipeline = yamlData['Pipeline']
   for index, item in enumerate(pipeline):
       if item[0]['Method'] == 'CreateConnection':
          item[0]['Parameter.HubUrl'] = hubUrls
   print(yaml.dump(yamlData))
    
def parse(input, urls):
    with open(input, 'r') as f:
        data = yaml.load(f)
        replaceHubUrl(data, urls)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the input Yaml file")
   parser.add_argument("-s", "--serverurls", help="Specify the hub server urls where were split by ','")
   args = parser.parse_args()
   if args.input is None or args.serverurls is None:
      print("Input file or server urls is not specified!")
   else:
      parse(args.input, args.serverurls)
