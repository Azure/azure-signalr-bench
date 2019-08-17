from bs4 import BeautifulSoup
import argparse
import os

def ParseXML(infile):
   with open(infile, "r") as f:
      contents = f.read()
      soup = BeautifulSoup(contents, "html.parser")
      triggers = soup.find("hudson.triggers.timertrigger")
      if soup.find('spec'):
        spec = triggers.spec
        print(spec.text)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="specify the input json result", required=True)
   args = parser.parse_args()

   ParseXML(args.input)
