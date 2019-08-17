import argparse
import datetime
import glob, os, re
from filter_utils import *

def FilterASRSLog(startDate, endDate):
    filterLog("/mnt/Data/NginxRoot", "*_ASRS.tgz", startDate, endDate)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-s", "--startDate",
          default=aWeekAgo(),
          help="specify the starting date to check, default is a week ago")
   parser.add_argument("-e", "--endDate",
          help="specify the ending date to check, default is today",
          default=today())
   args = parser.parse_args()
   FilterASRSLog(args.startDate, args.endDate)
