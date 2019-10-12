import sys

others={1:2, 2:2, 5:4, 10:4, 20:8, 50:20, 100:30}
echo={1:2, 2:2, 5:4, 10:4, 20:8, 50:32, 100:32}
sendToClient=others
sendToGroup=others
streamingEcho=others
broadcast={1:2, 2:2, 5:2, 10:2, 20:2, 50:2, 100:2}

def unit1_webappserver(s):
   return s[1]

def unit2_webappserver(s):
   return s[2]

def unit5_webappserver(s):
   return s[5]

def unit10_webappserver(s):
   return s[10]

def unit20_webappserver(s):
   return s[20]

def unit50_webappserver(s):
   return s[50]

def unit100_webappserver(s):
   return s[100]

