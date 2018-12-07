import datetime

def yearMonthDayFmt(date):
    return date.strftime("%Y") + date.strftime("%m") + date.strftime("%d")

def today():
    return yearMonthDayFmt(datetime.date.today())

def aWeekAgo():
    today = datetime.date.today()
    weekago = today - datetime.timedelta(days=7)
    return yearMonthDayFmt(weekago)

