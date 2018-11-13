import argparse
import sys

def gen_google_chart_table_summary(input, sep):
   head="""
      google.charts.load("current", {packages:["table"]});
      google.charts.setOnLoadCallback(drawWarnDetails);
      function drawWarnDetails() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Date');
        data.addColumn('string', 'Scenario');
        data.addColumn('number', 'Errors');
        data.addColumn('string', 'TopError');
        data.addRows(["""
   print(head)
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          fields = line.rstrip().split(sep)
          assert len(fields) == 5, "Invalid input file: the columns do not match requirement {len}".format(len=len(fields))
          date = fields[0]
          scenario = fields[1]
          errorCount = fields[2]
          topError = fields[3]
          link = fields[4]
          content="""          ['{d}', '{s}', {ec}, '<a href="{link}">{te}</a>'],""".format(d=date, s=scenario, ec=errorCount, te=topError, link=link)
          print(content)
   tail="""
        ]);
        data.setColumnProperty(1, {allowHtml: true});
        var table = new google.visualization.Table(document.getElementById('1s_percent_table_div'));

        table.draw(data, options);
      }
"""
   print(tail)

def gen_google_chart_table_details(input, sep):
   head="""
      google.charts.load("current", {packages:["table"]});
      google.charts.setOnLoadCallback(drawWarnDetails);
      function drawWarnDetails() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Template');
        data.addColumn('string', 'ErrorDetails');
        data.addRows(["""
   print(head)
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          fields = line.rstrip().split(sep)
          assert len(fields) == 2, "Invalid input file: the columns do not match requirement {len}".format(len=len(fields))
          tmpl = fields[0]
          details = fields[1]
          content="""          ['{tmpl}', '{details}'],""".format(tmpl=tmpl, details=details)
          print(content)
   tail="""
        ]);
        data.setColumnProperty(1, {allowHtml: true});
        var table = new google.visualization.Table(document.getElementById('warn_details_table_div'));

        table.draw(data, options);
      }
"""
   print(tail)

def gen_google_chart_table_counter(input, sep):
   head="""
      google.charts.load("current", {packages:["table"]});
      google.charts.setOnLoadCallback(drawWarnCount);
      function drawWarnCount() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Template');
        data.addColumn('number', 'Percentage');
        data.addColumn('number', 'Count');
        data.addRows(["""
   print(head)
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          fields = line.rstrip().split(sep)
          assert len(fields) == 3, "Invalid input file: the columns do not match requirement {len}".format(len=len(fields))
          tmpl = fields[0]
          percent = fields[1]
          count = fields[2]
          content="""          ['{tmpl}', {percent}, {count}],""".format(tmpl=tmpl, percent=percent, count=count)
          print(content)
   tail="""
        ]);
        data.setColumnProperty(1, {allowHtml: true});
        var table = new google.visualization.Table(document.getElementById('warn_count_table_div'));

        table.draw(data, options);
      }
"""
   print(tail)

if __name__=="__main__":
   parser = argparse.ArgumentParser()
   parser.add_argument("-i", "--input", help="Specify the file contains <template,percentage,count> information")
   parser.add_argument("-g", "--generate", type=str, choices=["summary", "counter", "details"], default="counter", help="Which kind of html do you want to generate. Default is counter.")
   parser.add_argument("-s", "--separator", help="Specify the separator for record, default is ','", default=",")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      func="gen_google_chart_table_{arg}(args.input,args.separator)".format(arg=args.generate)
      eval(func)
