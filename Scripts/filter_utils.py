import datetime
import fnmatch
import os
import re

def yearMonthDayFmt(date):
    return date.strftime("%Y") + date.strftime("%m") + date.strftime("%d")

def today():
    return yearMonthDayFmt(datetime.date.today())

def aWeekAgo():
    today = datetime.date.today()
    weekago = today - datetime.timedelta(days=7)
    return yearMonthDayFmt(weekago)

# wildcardPat: {*a,*b}
def filterTargetFile(rootDir, wildcardPat, callback, *args):
    for root, dirs, files in os.walk(rootDir):
        for file in fnmatch.filter(files, wildcardPat):
            callbackInput = os.path.join(root, file)
            callback(callbackInput, *args)

def internalLogCallback(filePath, startDate, endDate):
   startIntValue = int(startDate+"000000")
   endIntValue = int(endDate+"235959")
   b = filePath.split("/")
   pat = re.compile(r"[0-9]+")
   if (len(b) >= 6 and pat.match(b[4])):
      tgtDate = int(b[4])
      if (tgtDate >= startIntValue and tgtDate < endIntValue):
         c = "{d} {unit} {path}".format(d=b[4], unit=b[5], path=filePath)
         print(c)

def filterLog(rootDir, wildcardPat, startDate, endDate):
   filterTargetFile(rootDir, wildcardPat, internalLogCallback, startDate, endDate)
