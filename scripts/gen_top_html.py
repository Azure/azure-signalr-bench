import argparse
import os
import sys

def gen_table_column_name(titleLine, sep):
   fields = titleLine.rstrip().split(sep)
   l = len(fields)
   for i, val in enumerate(fields):
     print("   data.addColumn('number', '{name}');".format(name=val))

def gen_google_chart_table(inputFile, column):
   fname = os.path.basename(os.path.splitext(inputFile)[0])
   print(fname)
   header="""
   google.charts.load('current', {packages: ['corechart', 'line']});
   google.charts.setOnLoadCallback(drawCurveTypes);
   function drawCurveTypes() {
      var data = new google.visualization.DataTable();
      data.addColumn('number', 'X');
"""
   print (header)
   # print columns
   with open(inputFile, 'r') as f:
     titleLine = [next(f) for x in xrange(1)]
     gen_table_column_name(titleLine[0], ',')
   # print date
   print ("   data.addRows([")
   with open(inputFile, 'r') as f:
     for i, line in enumerate(f):
         if (i==0):
            continue
         val="    [{l},{v}],".format(l=i-1,v=line.rstrip())
         print(val)
   # print tail
   divname="{f}_div".format(f=fname)
   tail="""
      ]);
      var options = {{
        hAxis: {{
          title: 'Time'
        }},
        vAxis: {{
          title: '{name}'
        }},
        series: {{
          1: {{curveType: 'function'}}
        }}
      }};

      var chart = new google.visualization.LineChart(document.getElementById('{divname}'));
      chart.draw(data, options);
    }}
""".format(name=fname, divname=divname)
   print(tail)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the file contains <date,scenario,connection,send,link> information")
   parser.add_argument("-c", "--column", help="Specify the column number")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      gen_google_chart_table(args.input, args.column)
