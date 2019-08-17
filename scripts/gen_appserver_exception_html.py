import argparse
import sys

def gen_google_chart_table_summary(input, sep):
   head="""
      google.charts.load("current", {packages:["table"]});
      google.charts.setOnLoadCallback(drawAppServerException);
      function drawAppServerException() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Date');
        data.addColumn('string', 'Scenario');
        data.addColumn('string', 'Errors');
        data.addRows(["""
   print(head)
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          fields = line.rstrip().split(sep)
          assert len(fields) == 4, "Invalid input file: the columns do not match requirement {len}".format(len=len(fields))
          date = fields[0]
          scenario = fields[1]
          errorCount = fields[2]
          link = fields[3]
          content="""          ['{d}', '{s}', '<a href="{link}">{ec}</a>'],""".format(d=date, s=scenario, ec=errorCount, link=link)
          print(content)
   tail="""
        ]);
        data.setColumnProperty(1, {allowHtml: true});
        var table = new google.visualization.Table(document.getElementById('1s_percent_table_div'));

        table.draw(data, options);
      }
"""
   print(tail)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the file contains <template,percentage,count> information")
   parser.add_argument("-s", "--separator", help="Specify the separator for record, default is ','", default=",")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      gen_google_chart_table_summary(args.input,args.separator)
