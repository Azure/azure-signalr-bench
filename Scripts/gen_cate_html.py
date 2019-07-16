import argparse
import sys

def gen_google_chart_table(input):
   maxField = 0
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          if (line.isspace()):
            continue
          fields = line.rstrip().split(',')
          if (len(fields) > maxField):
              maxField = len(fields)
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
        data.addColumn('number', 'SendTPuts');
        data.addColumn('number', 'RecvTPuts');"""
   print(head)
   if (maxField == 10):
        append="""
        data.addColumn('number', 'ConnectionDrop');
        data.addColumn('number', '99%ReconnCost(ms)');
        data.addColumn('number', '99%LifeSpan(ms)');"""
        print(append)
   print("data.addRows([")
   with open(input, 'r') as f:
      for i,line in enumerate(f):
          if (line.isspace()):
            continue
          fields = line.rstrip().split(',')
          if (len(fields) < 7):
            print("Invalid input line '{a}': the columns do not match requirement".format(a=line))
          assert len(fields) >= 7, "Invalid input line : the columns do not match requirement"
          d = fields[0]
          scenario = fields[1]
          conn = fields[2]
          send = fields[3]
          sendTPuts = fields[4]
          recvTPuts = fields[5]
          link = fields[6]
          if (len(fields) == 10):
             drop = fields[7]
             reconnCost = fields[8]
             lifeSpan = fields[9]
          if (len(fields) == 7):
             content="""          ['{date}', '<a href="{link}">{scenario}</a>', {conn}, {send}, {sendTPuts}, {recvTPuts}],""".format(date=d,link=link,conn=conn,send=send,scenario=scenario,sendTPuts=sendTPuts,recvTPuts=recvTPuts)
          elif (len(fields) == 10):
             content="""          ['{date}', '<a href="{link}">{scenario}</a>', {conn}, {send}, {sendTPuts}, {recvTPuts}, {drop}, {reconnCost}, {lifeSpan}],""".format(date=d,link=link,conn=conn,send=send,scenario=scenario,sendTPuts=sendTPuts,recvTPuts=recvTPuts,drop=drop,reconnCost=reconnCost,lifeSpan=lifeSpan)
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
   parser.add_argument("-i", "--input", help="Specify the file contains <date,scenario,connection,send,sendTPuts,recvTPuts,link> information")
   args = parser.parse_args()
   if args.input is None:
      print("Input file is not specified!")
   else:
      gen_google_chart_table(args.input)
