
def init():
    global gConstNormal
    global gConstMax
    global gPerfType 
    gConstNormal = "normal"
    gConstMax    = "max"
    gPerfType    = gConstNormal # "normal" and "max"

def setMaxConnections():
    global gPerfType
    gPerfType = gConstMax
