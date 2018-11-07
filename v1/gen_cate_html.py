import argparse
import sys

def gen_google_chart_table(input):
   head="""
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(draw1sPercent);
      function draw1sPercent() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'DateTimestamp');
        data.addColumn('string', 'Scenario');
        data.addColumn('number', 'Connections');
        data.addColumn('number', 'Send');
        data.addRows(["""
   print(head)
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          fields = line.rstrip().split(',')
          assert len(fields) == 5, "Invalid input file: the columns do not match requirement"
          d = fields[0]
          scenario = fields[1]
          conn = fields[2]
          send = fields[3]
          link = fields[4]
          content="""          ['{date}', '<a href="{link}">{scenario}</a>', {conn}, {send}],""".format(date=d,link=link,conn=conn,send=send,scenario=scenario)
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
   parser.add_argument("-i", "--input", help="Specify the file contains <date,scenario,connection,send,link> information")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      gen_google_chart_table(args.input)
