package main

import (
	"bytes"
	"encoding/json"
	"flag"
	"fmt"
	"html/template"
	"io/ioutil"
	"strings"
	"time"
)

type Datapoint struct {
	TimestampUtc string  `json:"TimestampUtc"`
	Value        float32 `json:"Value"`
}

type Dimension struct {
	Key   string `json:"Key"`
	Value string `json:"Value"`
}

type Id struct {
	MetricName        string `json:"MetricName"`
	MetricNamespace   string `json:"MetricNamespace"`
	MonitoringAccount string `json:"MonitoringAccount"`
}

type Definition struct {
	AggregationType      int         `json:"AggregationType"`
	DimensionCombination []Dimension `json:"DimensionCombination"`
	EndTimeUtc           string      `json:"EndTimeUtc"`
	Id                   Id          `json:"Id"`
	SamplingTypes        []struct {
		Name string `json:"Name"`
	} `json:"SamplingTypes"`
	StartTimeUtc string `json:"StartTimeUtc"`
}

type TimeSeries struct {
	Definition   Definition  `json:"Definition"`
	Datapoints   []Datapoint `json:"Datapoints"`
	EndTimeUtc   string      `json:"EndTimeUtc"`
	ErrorCode    int         `json:"ErrorCode"`
	StartTimeUtc string      `json:"StartTimeUtc"`
}

// require .FuncName, .MetricName
const chart_template1 = `
      google.charts.load("current", {packages:["corechart", "line", "table"]});
      google.charts.setOnLoadCallback({{.FuncName}});
      function {{.FuncName}}() {
        var data = new google.visualization.DataTable();
        data.addColumn('timeofday', 'Time');
        data.addColumn('number', '{{.MetricName}}');
        data.addRows([
        `

// require .Title, .SubTitle, .ElementId
const chart_template2 = `
        ]);
      var options = {
        chart: {
          title: '{{.Title}}',
          subtitle: '{{.SubTitle}}'
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

      var chart = new google.charts.Line(document.getElementById('{{.ElementId}}'));

      chart.draw(data, google.charts.Line.convertOptions(options));
    }
	`

func gen_func(str string, data map[string]interface{}) string {
	t1 := template.Must(template.New("template1").Parse(str))
	buf := &bytes.Buffer{}
	if err := t1.Execute(buf, data); err != nil {
		panic(err)
	}
	return buf.String()
}

func main() {
	var infile = flag.String("input", "", "Specify the input file")
	var inElementIndex = flag.String("index", "metrics_cpu_memory", "Specify the index")
	flag.Usage = func() {
		fmt.Println("-input <input_file> : specify the input file")
		fmt.Println("-index <pod_index> : specify the index of the pod")
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
	var timeSeries TimeSeries
	json.Unmarshal(raw, &timeSeries)
	// dimension information
	var rawBuffer bytes.Buffer
	var buffer bytes.Buffer
	for _, dimension := range timeSeries.Definition.DimensionCombination {
		buffer.WriteString(dimension.Key)
		buffer.WriteString("_")
		buffer.WriteString(strings.Replace(dimension.Value, "-", "_", -1))
		// generate raw dimention
		rawBuffer.WriteString(dimension.Key)
		rawBuffer.WriteString(":")
		rawBuffer.WriteString(dimension.Value)
	}
	dimensionName := buffer.String()
	// metric information
	buffer.Reset()
	buffer.WriteString(timeSeries.Definition.Id.MonitoringAccount)
	buffer.WriteString(":")
	buffer.WriteString(timeSeries.Definition.Id.MetricNamespace)
	buffer.WriteString(":")
	buffer.WriteString(timeSeries.Definition.Id.MetricName)
	metricName := buffer.String()

	map1 := map[string]interface{}{
		"FuncName":   "draw" + dimensionName,
		"MetricName": metricName,
	}
	result := gen_func(chart_template1, map1)
	fmt.Printf("%s\n", result)
	for _, datapoint := range timeSeries.Datapoints {
		t1, _ := time.Parse(time.RFC3339, datapoint.TimestampUtc+"+00:00")
		fmt.Printf("\t [[%d, %d, %d], %.0f],\n", t1.Hour(), t1.Minute(), t1.Second(), datapoint.Value)
	}
	map2 := map[string]interface{}{
		"Title":     timeSeries.Definition.Id.MetricName,
		"SubTitle":  rawBuffer.String(),
		"ElementId": inElementIndex,
	}
	result2 := gen_func(chart_template2, map2)
	fmt.Printf("%s\n", result2)
}
