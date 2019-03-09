package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"io/ioutil"
	"time"
)

type Counters struct {
	InProgress  int64 `json:"connection:inprogress"`
	Established int64 `json:"connection:established"`
	Error       int64 `json:"error"`
	Success     int64 `json:"success"`
	Send        int64 `json:"message:sent"`
	Recv        int64 `json:"message:received"`
	SendSize    int64 `json:"message:sentSize"`
	RecvSize    int64 `json:"message:recvSize"`
	LT_100      int64 `json:"message:lt:100"`
	LT_200      int64 `json:"message:lt:200"`
	LT_300      int64 `json:"message:lt:300"`
	LT_400      int64 `json:"message:lt:400"`
	LT_500      int64 `json:"message:lt:500"`
	LT_600      int64 `json:"message:lt:600"`
	LT_700      int64 `json:"message:lt:700"`
	LT_800      int64 `json:"message:lt:800"`
	LT_900      int64 `json:"message:lt:900"`
	LT_1000     int64 `json:"message:lt:1000"`
	GE_1000     int64 `json:"message:ge:1000"`
	Sending     int64 `json:"sendingStep"`
	ConnError   int64 `json:"connection:connect:fail"`
	ReConn      int64 `json:"connection:connect:reconnect"`
	ConnSucc    int64 `json:"connection:connect:success"`
}

type Monitor struct {
	Timestamp string `json:"Time"`
	Counters  Counters
}

func main() {
	var infile = flag.String("input", "", "Specify the input file")
	var timeWindow, lastLatency, all, rate, sizerate, lastLatab, lastLatabPercent, category500ms, category1s, areachart, connectrate bool
	lastLatency = false
	all = false
	rate = false
	sizerate = false
	lastLatab = false
	lastLatabPercent = false
	category500ms = false
	category1s = false
	areachart = false
	connectrate = false
	timeWindow = false
	flag.BoolVar(&all, "all", false, "Print all information")
	flag.BoolVar(&lastLatency, "lastlatency", false, "Print the last item of latency")
	flag.BoolVar(&rate, "rate", false, "Print send/recv rate")
	flag.BoolVar(&sizerate, "sizerate", false, "Print send/recv size rate")
	flag.BoolVar(&lastLatab, "lastlatab", false, "Print a table for last latency")
	flag.BoolVar(&lastLatabPercent, "lastlatabPercent", false, "Print a table for last latency percentage")
	flag.BoolVar(&category500ms, "category500ms", false, "Print a table for last latency percentage with 500ms as a boundary")
	flag.BoolVar(&category1s, "category1s", false, "Print a table for last latency percentage with 1s as a boundary")
	flag.BoolVar(&areachart, "areachart", false, "Print Area chart")
	flag.BoolVar(&timeWindow, "timeWindow", false, "Print the time window for test [start_time - 1 minutes] and [end_time + 1 mintues]")
	flag.BoolVar(&connectrate, "connectrate", false, "Print the connection rate")
	flag.Usage = func() {
		fmt.Println("-input <input_file> : specify the input file")
		fmt.Println("-lastlatency        : print the last item of latency")
		fmt.Println("-all         : print all information")
		fmt.Println("-rate               : print send/recv rate")
		fmt.Println("-sizerate           : print send/recv message size rate")
		fmt.Println("-lastlatab          : print a table for last latency")
		fmt.Println("-lastlatabPercent   : print a table for last latency percentage")
		fmt.Println("-category500ms      : print a table for last latency percentage with 500ms as a boundry")
		fmt.Println("-category1s         : print a table for last latency percentage with 1s as a boundry")
		fmt.Println("-areachart          : print Area chart")
		fmt.Println("-connectrate        : print connect rate chart")
		fmt.Println("-timeWindow         : Print the time window for test [start_time - 1 minutes] and [end_time + 1 mintues]")
	}
	flag.Parse()
	if infile == nil || *infile == "" {
		fmt.Println("No input")
		flag.Usage()
		return
	}
	raw, err := ioutil.ReadFile(*infile)
	if err != nil {
		fmt.Println(err.Error())
		return
	}
	var monitors []Monitor
	er := json.Unmarshal(raw, &monitors)
	if er != nil {
		fmt.Printf("Failed to parse json data: %s\n", er.Error())
		return
	}
	if timeWindow && len(monitors) > 0 {
		PrintTimeWindow(monitors)
	}
	if all {
		PrintAll(monitors)
	}
	if areachart {
		PrintAreaChart(monitors)
	}
	if lastLatency {
		PrintLastLatency(removeInvalidCounters(monitors))
	}
	if lastLatab {
		PrintLastLatab(removeInvalidCounters(monitors))
	}
	if lastLatabPercent {
		PrintLastLatabPercent(removeInvalidCounters(monitors))
	}
	if category500ms {
		PrintCategory500ms(removeInvalidCounters(monitors))
	}
	if category1s {
		PrintCategory1s(removeInvalidCounters(monitors))
	}
	if rate {
		PrintRate(monitors)
	}
	if connectrate {
		PrintConnectRate(monitors)
	}
	if sizerate {
		PrintSizeRate(monitors)
	}
}

func PrintAll(monitors []Monitor) {
	for _, v := range monitors {
		// timestamp succ err inprogress send recv sendSize recvSize lt_100 lt_200 lt_300 lt_400 lt_500 lt_600 lt_700 lt_800 lt_900 lt_1000 ge_1000
		fmt.Printf("%d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d\n",
			v.Timestamp, v.Counters.Established, v.Counters.Error, v.Counters.InProgress,
			v.Counters.Send,
			v.Counters.Recv,
			v.Counters.SendSize,
			v.Counters.RecvSize,
			v.Counters.LT_100,
			v.Counters.LT_200,
			v.Counters.LT_300,
			v.Counters.LT_400,
			v.Counters.LT_500,
			v.Counters.LT_600,
			v.Counters.LT_700,
			v.Counters.LT_800,
			v.Counters.LT_900,
			v.Counters.LT_1000,
			v.Counters.GE_1000)
	}
}

func PrintTimeWindow(monitors []Monitor) {
	startTime, _ := time.Parse(time.RFC3339, monitors[0].Timestamp)
	endTime, _ := time.Parse(time.RFC3339, monitors[len(monitors)-1].Timestamp)
	fmt.Printf("%s %s", startTime.Format(time.RFC3339), endTime.Format(time.RFC3339))
}

func hasSendingStep(monitors []Monitor) bool {
	var hasSendingStep bool
	hasSendingStep = false
	for _, v := range monitors {
		if v.Counters.Sending != 0 {
			hasSendingStep = true
			break
		}
	}
	return hasSendingStep
}

func removeInvalidCounters(monitors []Monitor) []Monitor {
	var ret = make([]Monitor, 0, len(monitors))
	var curSendingStep, preSend int64
	preSend = 0
	curSendingStep = 0
	for _, v := range monitors {
		if (curSendingStep == 0 || curSendingStep == v.Counters.Sending) &&
			v.Counters.Send >= preSend && v.Counters.Send > 0 {
			ret = append(ret, v)
		}
		curSendingStep = v.Counters.Sending
		preSend = v.Counters.Send
	}
	return ret
}

func PrintLastLatabPercent(monitors []Monitor) {
	var sum int64
	var totalConnection int64
	var v Monitor
	if hasSendingStep(monitors) {
		var chartfunc string
		chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(drawLastLatencyPercent);
      function drawLastLatencyPercent() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('number', 'Sending');
        data.addColumn('number', 'LT100ms(%)');
        data.addColumn('number', 'LT200ms(%)');
        data.addColumn('number', 'LT300ms(%)');
        data.addColumn('number', 'LT400ms(%)');
        data.addColumn('number', 'LT500ms(%)');
        data.addColumn('number', 'LT600ms(%)');
        data.addColumn('number', 'LT700ms(%)');
        data.addColumn('number', 'LT800ms(%)');
        data.addColumn('number', 'LT900ms(%)');
        data.addColumn('number', 'LT1000ms(%)');
        data.addColumn('number', 'GE1000ms(%)');
        data.addColumn('number', 'ConnectionDropped(%)');
        data.addColumn('number', 'Reconnect(%)');
        `
		fmt.Printf("%s\n", chartfunc)
		fmt.Printf("\tdata.addRows([\n")
		var curSendingStep int64
		for i, v := range monitors {
			curSendingStep = v.Counters.Sending
			if i+1 < len(monitors) &&
				monitors[i+1].Counters.Sending != curSendingStep &&
				v.Counters.Recv > 0 {
				sum = v.Counters.Recv
				totalConnection = v.Counters.ConnError + v.Counters.ConnSucc
				var sumfloat, totalConnFloat float64
				sumfloat = float64(sum)
				totalConnFloat = float64(totalConnection)
				fmt.Printf("\t [%d, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f],\n",
					curSendingStep, float64(v.Counters.LT_100)/sumfloat*100,
					float64(v.Counters.LT_200)/sumfloat*100,
					float64(v.Counters.LT_300)/sumfloat*100, float64(v.Counters.LT_400)/sumfloat*100,
					float64(v.Counters.LT_500)/sumfloat*100, float64(v.Counters.LT_600)/sumfloat*100,
					float64(v.Counters.LT_700)/sumfloat*100, float64(v.Counters.LT_800)/sumfloat*100,
					float64(v.Counters.LT_900)/sumfloat*100, float64(v.Counters.LT_1000)/sumfloat*100,
					float64(v.Counters.GE_1000)/sumfloat*100, float64(v.Counters.ConnError)/totalConnFloat*100,
					float64(v.Counters.ReConn)/totalConnFloat*100)
			}
		}
		v = monitors[len(monitors)-1]
		if v.Counters.Recv != sum && v.Counters.Recv > 0 {
			var sumfloat, totalConnFloat float64
			sum = v.Counters.Recv
			totalConnection = v.Counters.ConnError + v.Counters.ConnSucc
			sumfloat = float64(sum)
			totalConnFloat = float64(totalConnection)
			fmt.Printf("\t [%d, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f, %.2f],\n", v.Counters.Sending,
				float64(v.Counters.LT_100)/sumfloat*100, float64(v.Counters.LT_200)/sumfloat*100,
				float64(v.Counters.LT_300)/sumfloat*100, float64(v.Counters.LT_400)/sumfloat*100,
				float64(v.Counters.LT_500)/sumfloat*100, float64(v.Counters.LT_600)/sumfloat*100,
				float64(v.Counters.LT_700)/sumfloat*100, float64(v.Counters.LT_800)/sumfloat*100,
				float64(v.Counters.LT_900)/sumfloat*100, float64(v.Counters.LT_1000)/sumfloat*100,
				float64(v.Counters.GE_1000)/sumfloat*100, float64(v.Counters.ConnError)/totalConnFloat*100,
				float64(v.Counters.ReConn)/totalConnFloat*100)
		}
		chartfunc = `
        ]);
        var table = new google.visualization.Table(document.getElementById('table_div'));

        table.draw(data, options);
      }
        `
		fmt.Printf("%s\n", chartfunc)
	}
}

func PrintLastLatab(monitors []Monitor) {
	var v Monitor
	v = monitors[len(monitors)-1]
	fmt.Printf("\tdata.addColumn('number', 'LT100ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT200ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT300ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT400ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT500ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT600ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT700ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT800ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT900ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'LT1000ms');\n")
	fmt.Printf("\tdata.addColumn('number', 'GE1000ms');\n")
	fmt.Printf("\tdata.addRows([\n")
	fmt.Printf("\t [%d, %d, %d, %d, %d, %d, %d, %d, %d, %d, %d],\n", v.Counters.LT_100, v.Counters.LT_200, v.Counters.LT_300,
		v.Counters.LT_400, v.Counters.LT_500, v.Counters.LT_600, v.Counters.LT_700, v.Counters.LT_800, v.Counters.LT_900,
		v.Counters.LT_1000, v.Counters.GE_1000)
	fmt.Printf("\t]);\n")
}

func PrintLastLatency(monitors []Monitor) {
	var v Monitor
	v = monitors[len(monitors)-1]
	var chartfunc string
	chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(drawDonut);
      function drawDonut() {
        var data = google.visualization.arrayToDataTable([
          ['Latency category', 'Counters'],
            `
	fmt.Printf("%s\n", chartfunc)
	fmt.Printf("\t ['LT100', %d],\n", v.Counters.LT_100)
	fmt.Printf("\t ['LT200', %d],\n", v.Counters.LT_200)
	fmt.Printf("\t ['LT300', %d],\n", v.Counters.LT_300)
	fmt.Printf("\t ['LT400', %d],\n", v.Counters.LT_400)
	fmt.Printf("\t ['LT500', %d],\n", v.Counters.LT_500)
	fmt.Printf("\t ['LT600', %d],\n", v.Counters.LT_600)
	fmt.Printf("\t ['LT700', %d],\n", v.Counters.LT_700)
	fmt.Printf("\t ['LT800', %d],\n", v.Counters.LT_800)
	fmt.Printf("\t ['LT900', %d],\n", v.Counters.LT_900)
	fmt.Printf("\t ['LT1000', %d],\n", v.Counters.LT_1000)
	fmt.Printf("\t ['GE1000', %d],\n", v.Counters.GE_1000)
	chartfunc = `
        ]);

        var options = {
          title: 'Latency distribution of the last second',
          is3D: true,
      width: 600,
          height: 400,
      backgroundColor: 'transparent'
        };

        var chart = new google.visualization.PieChart(document.getElementById('piechart_3d'));
        chart.draw(data, options);
      }
            `
	fmt.Printf("%s\n", chartfunc)
}

func PrintAreaChart(monitors []Monitor) {
	var chartfunc string
	chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(drawAreaChart);
      function drawAreaChart() {
        var data = google.visualization.arrayToDataTable([
['Send','LT100','LT200','LT300','LT400','LT500','LT600','LT700','LT800','LT900','LT1000','GE1000'],
            `
	fmt.Printf("%s\n", chartfunc)
	for _, v := range monitors {
		fmt.Printf("['%d', %d, %d, %d, %d, %d, %d, %d, %d, %d, %d, %d],\n",
			v.Counters.Send,
			v.Counters.LT_100,
			v.Counters.LT_200,
			v.Counters.LT_300,
			v.Counters.LT_400,
			v.Counters.LT_500,
			v.Counters.LT_600,
			v.Counters.LT_700,
			v.Counters.LT_800,
			v.Counters.LT_900,
			v.Counters.LT_1000,
			v.Counters.GE_1000)
	}
	chartfunc = `
        ]);

        var options = {
          title: 'Latency distribution of the whole message sending',
          hAxis: {title: 'Send count',  titleTextStyle: {color: '#333'}},
          vAxis: {minValue: 0},
      isStacked: 'relative',
      width: 1200,
          height: 600,
      backgroundColor: 'transparent'
        };

        var chart = new google.visualization.AreaChart(document.getElementById('area_div'));
        chart.draw(data, options);
      }
            `
	fmt.Printf("%s\n", chartfunc)
}

func PrintCategory1s(monitors []Monitor) {
	var v Monitor
	var sum, totalConnection, lt1, ge1 int64
	if hasSendingStep(monitors) {
		var chartfunc string
		chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(draw1sPercent);
      function draw1sPercent() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('number', 'Sending');
        data.addColumn('number', 'LT1s(%)');
        data.addColumn('number', 'GE1s(%)');
        data.addColumn('number', 'ConnectionDropped(%)');
        data.addColumn('number', 'Reconnect(%)');
            `
		fmt.Printf("%s\n", chartfunc)
		fmt.Printf("\tdata.addRows([\n")

		var curSendingStep int64
		var lastValidIndex int
		for i, v := range monitors {
			if v.Counters.Recv > 0 {
				curSendingStep = v.Counters.Sending
				if i+1 < len(monitors) &&
					monitors[i+1].Counters.Sending != curSendingStep {
					sum = v.Counters.Recv
					totalConnection = v.Counters.ConnError + v.Counters.ConnSucc
					var sumfloat, totalConnFloat float64
					sumfloat = float64(sum)
					totalConnFloat = float64(totalConnection)
					lt1 = v.Counters.LT_100 + v.Counters.LT_200 + v.Counters.LT_300 + v.Counters.LT_400 + v.Counters.LT_500 + v.Counters.LT_600 + v.Counters.LT_700 + v.Counters.LT_800 + v.Counters.LT_900 + v.Counters.LT_1000
					ge1 = v.Counters.GE_1000
					fmt.Printf("\t [%d, %.2f, %.2f, %.2f, %.2f],\n",
						curSendingStep, float64(lt1)/sumfloat*100,
						float64(ge1)/sumfloat*100, float64(v.Counters.ConnError)/totalConnFloat*100,
						float64(v.Counters.ReConn)/totalConnFloat*100)
				}
				lastValidIndex = i
			}
		}
		if lastValidIndex > 0 &&
			monitors[lastValidIndex].Counters.Recv != sum &&
			monitors[lastValidIndex].Counters.Recv > 0 {
			v = monitors[lastValidIndex]
			sum = v.Counters.Recv
			totalConnection = v.Counters.ConnError + v.Counters.ConnSucc
			var sumfloat, totalConnFloat float64
			sumfloat = float64(sum)
			totalConnFloat = float64(totalConnection)
			lt1 = v.Counters.LT_100 + v.Counters.LT_200 + v.Counters.LT_300 + v.Counters.LT_400 + v.Counters.LT_500 + v.Counters.LT_600 + v.Counters.LT_700 + v.Counters.LT_800 + v.Counters.LT_900 + v.Counters.LT_1000
			ge1 = v.Counters.GE_1000
			fmt.Printf("\t [%d, %.2f, %.2f, %.2f, %.2f],\n",
				v.Counters.Sending, float64(lt1)/sumfloat*100,
				float64(ge1)/sumfloat*100, float64(v.Counters.ConnError)/totalConnFloat*100,
				float64(v.Counters.ReConn)/totalConnFloat*100)
		}
		chartfunc = `
        ]);
        var table = new google.visualization.Table(document.getElementById('1s_percent_table_div'));

        table.draw(data, options);
      }
            `
		fmt.Printf("%s\n", chartfunc)
	}
}

func PrintCategory500ms(monitors []Monitor) {
	var sum, totalConnection, lt500, ge500 int64
	var v Monitor
	if hasSendingStep(monitors) {
		var chartfunc string
		chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(draw500msPercent);
      function draw500msPercent() {
        var cssClassNames = {headerCell: 'headerCell', tableCell: 'tableCell'};
        var options = {showRowNumber: true,'allowHtml': true, 'cssClassNames': cssClassNames, 'alternatingRowStyle': true};
        var data = new google.visualization.DataTable();
        data.addColumn('number', 'Sending');
        data.addColumn('number', 'LT500ms(%)');
        data.addColumn('number', 'GE500ms(%)');
        data.addColumn('number', 'ConnectionDropped(%)');
        data.addColumn('number', 'Reconnect(%)');
            `
		fmt.Printf("%s\n", chartfunc)
		fmt.Printf("\tdata.addRows([\n")

		var curSendingStep int64
		var lastValidIndex int
		for i, v := range monitors {
			if v.Counters.Recv > 0 {
				curSendingStep = v.Counters.Sending
				if i+1 < len(monitors) &&
					monitors[i+1].Counters.Sending != curSendingStep &&
					v.Counters.Recv > 0 {
					sum = v.Counters.Recv
					totalConnection = v.Counters.ConnError + v.Counters.ConnSucc
					var sumfloat, totalConnFloat float64
					sumfloat = float64(sum)
					totalConnFloat = float64(totalConnection)
					lt500 = v.Counters.LT_100 + v.Counters.LT_200 + v.Counters.LT_300 + v.Counters.LT_400 + v.Counters.LT_500
					ge500 = v.Counters.LT_600 + v.Counters.LT_700 + v.Counters.LT_800 + v.Counters.LT_900 + v.Counters.LT_1000 + v.Counters.GE_1000

					fmt.Printf("\t [%d, %.2f, %.2f, %.2f, %.2f],\n",
						curSendingStep, float64(lt500)/sumfloat*100,
						float64(ge500)/sumfloat*100,
						float64(v.Counters.ConnError)/totalConnFloat*100,
						float64(v.Counters.ReConn)/totalConnFloat*100)
				}
				lastValidIndex = i
			}
		}
		if lastValidIndex > 0 &&
			monitors[lastValidIndex].Counters.Recv != sum &&
			monitors[lastValidIndex].Counters.Recv > 0 {
			v = monitors[lastValidIndex]
			sum = v.Counters.Recv
			totalConnection = v.Counters.ConnError + v.Counters.ConnSucc
			var sumfloat, totalConnFloat float64
			sumfloat = float64(sum)
			totalConnFloat = float64(totalConnection)
			lt500 = v.Counters.LT_100 + v.Counters.LT_200 + v.Counters.LT_300 + v.Counters.LT_400 + v.Counters.LT_500
			ge500 = v.Counters.LT_600 + v.Counters.LT_700 + v.Counters.LT_800 + v.Counters.LT_900 + v.Counters.LT_1000 + v.Counters.GE_1000
			fmt.Printf("\t [%d, %.2f, %.2f, %.2f, %.2f],\n",
				v.Counters.Sending, float64(lt500)/sumfloat*100,
				float64(ge500)/sumfloat*100,
				float64(v.Counters.ConnError)/totalConnFloat*100,
				float64(v.Counters.ReConn)/totalConnFloat*100)
		}
		chartfunc = `
        ]);
        var table = new google.visualization.Table(document.getElementById('500ms_percent_table_div'));

        table.draw(data, options);
      }
            `
		fmt.Printf("%s\n", chartfunc)
	}

}

func PrintSizeRate(monitors []Monitor) {
	var chartfunc string
	chartfunc = `
    google.charts.load("current", {packages:["corechart", "line", "table"]});
    google.charts.setOnLoadCallback(drawSendRecvSizeChart);
    function drawSendRecvSizeChart() {

      var data = new google.visualization.DataTable();
        data.addColumn('timeofday', 'Time');
        data.addColumn('number', 'Send message size (Byte/Sec)');
        data.addColumn('number', 'Recv message size (Byte/Sec)');
        data.addRows([
            `
	fmt.Printf("%s\n", chartfunc)
	for i, j := 0, 1; j < len(monitors); i, j = i+1, j+1 {
		t1, _ := time.Parse(time.RFC3339, monitors[j].Timestamp)
		// ignore invalid (negative values) SendSizeDiff and RecvSizeDiff
		sszdiff := monitors[j].Counters.SendSize - monitors[i].Counters.SendSize
		rszdiff := monitors[j].Counters.RecvSize - monitors[i].Counters.RecvSize
		if sszdiff > 0 && rszdiff > 0 {
			fmt.Printf("\t [[%d, %d, %d], %d, %d],\n", t1.Hour(), t1.Minute(), t1.Second(), sszdiff, rszdiff)
		}
	}
	chartfunc = `
        ]);

      var options = {
        chart: {
          title: 'Send/Recv message size rate',
          subtitle: 'mesage size for sending and receiving per second'
        },
        width: 1200,
        height: 400,
        backgroundColor: 'transparent',
        axes: {
          x: {
            0: {side: 'bottom'}
          }
        }
      };

      var chart = new google.charts.Line(document.getElementById('send_recv_size'));

      chart.draw(data, google.charts.Line.convertOptions(options));
    }
            `
	fmt.Printf("%s\n", chartfunc)
}

func PrintConnectRate(monitors []Monitor) {
	var chartfunc string
	chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(drawConnectionRate);
      function drawConnectionRate() {
        var data = new google.visualization.DataTable();
        data.addColumn('timeofday', 'Time');
        data.addColumn('number', 'Connection rate');
        data.addRows([
            `
	fmt.Printf("%s\n", chartfunc)
	var repeatedCount int64
	var connSucc int64
	repeatedCount = 0
	connSucc = 0
	for i, j := 0, 1; j < len(monitors); i, j = i+1, j+1 {
		if monitors[j].Counters.ConnSucc > 0 {
			if monitors[j].Counters.ConnSucc != connSucc {
				t1, _ := time.Parse(time.RFC3339, monitors[j].Timestamp)
				// ignore invalid negative values
				cdiff := monitors[j].Counters.ConnSucc - monitors[i].Counters.ConnSucc
				if cdiff > 0 {
					fmt.Printf("\t [[%d, %d, %d], %d],\n", t1.Hour(), t1.Minute(), t1.Second(), cdiff)
				}
			} else if repeatedCount < 10 {
				repeatedCount++
			} else {
				// We saw cotinuous the same connection number,
				// then we are sure all connections have been established
				break
			}
			connSucc = monitors[j].Counters.ConnSucc
		}
	}
	chartfunc = `
        ]);
      var options = {
        chart: {
          title: 'Concurrent connection rate',
          subtitle: 'concurrent connection count per second'
        },
        width: 1200,
        height: 400,
    backgroundColor: 'transparent',
        axes: {
          x: {
            0: {side: 'bottom'}
          }
        }
      };

      var chart = new google.charts.Line(document.getElementById('connect_rate'));

      chart.draw(data, google.charts.Line.convertOptions(options));
    }
            `
	fmt.Printf("%s\n", chartfunc)
}

func PrintRate(monitors []Monitor) {
	var chartfunc string
	chartfunc = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback(drawSendRecvRate);
      function drawSendRecvRate() {
        var data = new google.visualization.DataTable();
        data.addColumn('timeofday', 'Time');
        data.addColumn('number', 'Send count rate');
        data.addColumn('number', 'Recv count rate');
        data.addRows([
            `
	fmt.Printf("%s\n", chartfunc)
	for i, j := 0, 1; j < len(monitors); i, j = i+1, j+1 {
		t1, _ := time.Parse(time.RFC3339, monitors[j].Timestamp)
		// ignore invalid negative values
		sdiff := monitors[j].Counters.Send - monitors[i].Counters.Send
		rdiff := monitors[j].Counters.Recv - monitors[i].Counters.Recv
		if sdiff > 0 && rdiff > 0 {
			fmt.Printf("\t [[%d, %d, %d], %d, %d],\n", t1.Hour(), t1.Minute(), t1.Second(), sdiff, rdiff)
		}
	}
	chartfunc = `
        ]);
      var options = {
        chart: {
          title: 'Send/Recv rate',
          subtitle: 'Message count for sending and receiving per second'
        },
        width: 1200,
        height: 400,
    backgroundColor: 'transparent',
        axes: {
          x: {
            0: {side: 'bottom'}
          }
        }
      };

      var chart = new google.charts.Line(document.getElementById('send_recv_rate'));

      chart.draw(data, google.charts.Line.convertOptions(options));
    }
            `
	fmt.Printf("%s\n", chartfunc)
}
