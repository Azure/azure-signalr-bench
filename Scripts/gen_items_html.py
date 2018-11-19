import argparse
import os
import sys

def gen_table_column_name(typicalLine, kvSep):
   fields = typicalLine.rstrip().split(' ')
   l = len(fields)
   assert l >= 2, "'{fs}' whose column is not greater or equal than 2".format(fs=line)
   for i, val in enumerate(fields[1:]):
      record = val.split(kvSep)
      lr = len(record)
      assert lr == 2, "expect format of 'key{s}value', but see {v}".format(s=kvSep, v=val)
      data = """        data.addColumn('number', '{name}');""".format(name=record[0])
      print(data)

def gen_google_chart_table_items(input, sep):
   head="""
      google.charts.load("current", {packages:["table"]});
      google.charts.setOnLoadCallback(drawWarnDetails);
      function drawWarnDetails() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Name');"""
   print(head)

   with open(input, 'r') as f:
      # select the first line as sample line
      sampleLine = [next(f) for x in xrange(1)]
      gen_table_column_name(sampleLine[0], sep)

   print("""        data.addRows([""")

   fname = os.path.basename(os.path.splitext(input)[0])
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          fields = line.rstrip().split(' ')
          lr = len(fields)
          assert lr >= 2, "expect column >= 2, but see {lr}".format(lr=lr)
          data = "{a}".format(a="'" + fname + "_" + fields[0] + "'")
          for j, val in enumerate(fields[1:]):
              values = val.split(sep)
              if len(data) == 0:
                 data = "{d}".format(d=values[1])
              else:
                 data = data + ", {d}".format(d=values[1])
          row="""          [{d}],""".format(d=data)
          print(row)

   tail="""
        ]);
        data.setColumnProperty(1, {allowHtml: true});"""
   print(tail)
   tail="""
        var table = new google.visualization.Table(document.getElementById('{fname}_table_div'));
""".format(fname=fname)
   print(tail)
   tail="""
        table.draw(data, options);
      }"""
   print(tail)


if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the file contains <template,percentage,count> information")
   parser.add_argument("-s", "--separator", help="Specify the separator for record's key value separator, default is ':'", default=":")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      gen_google_chart_table_items(args.input,args.separator)
