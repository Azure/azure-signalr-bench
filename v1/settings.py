
def init():
    global gConstNormal
    global gConstMax
    global gPerfType
    global gInterval
    gConstNormal = "normal"
    gConstMax    = "max"
    gPerfType    = gConstNormal # "normal" and "max"
    gInterval    = 1

def setMaxConnections():
    global gPerfType
    gPerfType = gConstMax

def setInterval(interval):
    global gInterval
    gInterval = interval
