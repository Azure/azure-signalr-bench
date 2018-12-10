import argparse
import datetime
import glob, os, re
from filter_utils import *


if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-s", "--startDate",
          default=aWeekAgo(),
          help="specify the starting date to check, default is a week ago")
   parser.add_argument("-e", "--endDate",
          help="specify the ending date to check, default is today",
          default=today())
   parser.add_argument("-w", "--wildcard",
          type=str,
          choices=["log_appserver*"],
          help="specify the file prefix, default is log_appserver",
          default="log_appserver*")
   args = parser.parse_args()
   filterLog("/mnt/Data/NginxRoot",
             args.wildcard,
             args.startDate,
             args.endDate)
