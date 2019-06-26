import glob, os, re
import argparse

def FindCounters(targetDir):
    pat = re.compile(r"[0-9]+")
    for root, dirs, files in os.walk(targetDir):
        for file in files:
           if file.endswith("counters.txt"):
              a = os.path.join(root, file)
              b = a.split("/")
              if (len(b)==7 and pat.match(b[4])):
                 c = "{date} {scenario} {path}".format(date=b[4],scenario=b[5], path=a)
                 print(c)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-q", "--queryDir", default="/mnt/Data/NginxRoot", help="Specify the query directory")
   args = parser.parse_args()
   FindCounters(args.queryDir)
