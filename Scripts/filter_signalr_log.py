import argparse
import datetime
import glob, os, re

def ASRSLog(startDate, endDate):
    startIntValue = int(startDate+"000000")
    endIntValue = int(endDate+"235959")
    pat = re.compile(r"[0-9]+")
    for root, dirs, files in os.walk("/mnt/Data/NginxRoot"):
        for file in files:
            if file.endswith("_ASRS.tgz") or file.endswith("_ASRS.txt"):
               a = os.path.join(root, file)
               b = a.split("/")
               if (len(b) >= 6 and pat.match(b[4])):
                  tgtDate = int(b[4])
                  if (tgtDate >= startIntValue and tgtDate < endIntValue):
                     c = "{d} {unit} {path}".format(d=b[4], unit=b[5], path=a)
                     print(c)

def yearMonthDayFmt(date):
    return date.strftime("%Y") + date.strftime("%m") + date.strftime("%d")

def today():
    return yearMonthDayFmt(datetime.date.today())

def aWeekAgo():
    today = datetime.date.today()
    weekago = today - datetime.timedelta(days=7)
    return yearMonthDayFmt(weekago)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-s", "--startDate",
          default=aWeekAgo(),
          help="specify the starting date to check, default is a week ago")
   parser.add_argument("-e", "--endDate",
          help="specify the ending date to check, default is today",
          default=today())
   args = parser.parse_args()
   ASRSLog(args.startDate, args.endDate)
