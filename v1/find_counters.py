import glob, os, re

def FindCounters():
    pat = re.compile(r"[0-9]+")
    for root, dirs, files in os.walk("/mnt/Data/NginxRoot"):
        for file in files:
           if file.endswith("counters.txt"):
              a = os.path.join(root, file)
              b = a.split("/")
              if (len(b)==7 and pat.match(b[4])):
                 c = "{date} {scenario} {path}".format(date=b[4],scenario=b[5], path=a)
                 print(c)

FindCounters()
