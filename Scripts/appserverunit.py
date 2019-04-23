import sys

others={1:2, 2:2, 5:2, 10:2, 20:3, 50:10, 100:20}
echo=others
sendToClient=others
sendToGroup=others
broadcast={1:2, 2:2, 5:2, 10:2, 20:2, 50:2, 100:2}
restSendToUser={1:1, 2:1, 5:1, 10:1, 20:1, 50:1, 100:1}
restBroadcast=restSendToUser
restSendToGroup=restSendToUser
restPersistBroadcast=restSendToUser
restPersistSendToGroup=restSendToUser
restPersistSendToUser=restSendToUser

def unit1_appserver(s):
   return s[1]

def unit2_appserver(s):
   return s[2]

def unit5_appserver(s):
   return s[5]

def unit10_appserver(s):
   return s[10]

def unit20_appserver(s):
   return s[20]

def unit50_appserver(s):
   return s[50]

def unit100_appserver(s):
   return s[100]

