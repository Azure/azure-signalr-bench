import sys

others={1:4, 2:4, 5:6, 10:6, 20:10, 50:20, 100:30}
echo={1:4, 2:4, 5:6, 10:6, 20:10, 50:36, 100:60}
sendToClient=others
sendToGroup=others
broadcast={1:4, 2:4, 5:4, 10:4, 20:4, 50:4, 100:4}

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

