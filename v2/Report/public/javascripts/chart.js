var chartColors = {
    red: 'rgb(255, 99, 132)',
    orange: 'rgb(255, 159, 64)',
    yellow: 'rgb(255, 205, 86)',
    green: 'rgb(75, 192, 192)',
    blue: 'rgb(54, 162, 235)',
    purple: 'rgb(153, 102, 255)',
    grey: 'rgb(201, 203, 207)',
    black: 'rgb(0, 0, 0)',
    brown: 'rgb(150, 75, 0)',
    chartreuse: 'rgb(223, 255, 0)',
    phlox: 'rgb(223, 0, 255)'
};

var backgroundColor = [
    chartColors.red,
    chartColors.orange,
    chartColors.yellow,
    chartColors.green,
    chartColors.blue,
    chartColors.purple,
    chartColors.brown,
    chartColors.chartreuse,
    chartColors.phlox,
    chartColors.grey,
    chartColors.black
];

var labels = [
    'message:lt:100',
    'message:lt:200',
    'message:lt:300',
    'message:lt:400',
    'message:lt:500',
    'message:lt:600',
    'message:lt:700',
    'message:lt:800',
    'message:lt:900',
    'message:lt:1000',
    'message:ge:1000'
];


function getXLabel(counters) {
    var xLabels = [];
    counters.forEach((counter) => {
        xLabels.push(parseTime(counter['Time']));
    });
    return xLabels;
}

function createPieChart(results) {
    // pie charts
    window.pieCharts = [];
    for (var scenario in results) {
        var counters = results[scenario];
        var pieConfig = {
            type: "pie",
            data: {
                datasets: [{
                    data: [],
                    backgroundColor: [],
                    label: 'Latency'
                }],
                labels: []
            },
            options: {
                responsive: true,
                legend: {
                    position: 'right'
                },
                title: {
                    display: true,
                    text: 'Total Latency Distribution'
                }

            }
        };

        var dataset = { data: [], backgroundColor: backgroundColor, label: scenario };

        var data = [];
        var datasets = [];
        var counters = results[scenario];
        var counter = counters[counters.length - 1]['Counters'];
        labels.forEach(label => {
            data.push(counter[label]);
        });
        data.push(counter["message:sent"] - counter["message:received"]);
        dataset.data = data;
        datasets.push(dataset);
        pieConfig.data.datasets = datasets;
        pieConfig.data.labels = labels;
        pieConfig.data.labels = labels.concat("not received");
        var ctx = document.getElementById(`chart-area-${scenario}-pie`).getContext('2d');
        var pieChart = new Chart(ctx, pieConfig);
        pieCharts.push(pieChart);
    }
}

function createLatencyLineChart(results) {
    // line chart
    var lineCharts = [];
    for (var scenario in results) {
        var counters = results[scenario];

        var lineConfig = {
            type: "line",
            data: {
                labels: [],
                datasets: []
            },
            options: {
                responsive: true,
                legend: {
                    position: 'right'
                },
                title: {
                    display: true,
                    text: 'Latency Distribution Of Received Massages In Time'
                },
                //- maintainAspectRatio: false,
                //- spanGaps: false,
                elements: {
                    line: {
                        tension: 0.000001
                    },
                    point: {
                        radius: 0
                    }
                },
                scales: {
                    yAxes: [{
                        stacked: true
                    }]
                },
                plugins: {
                    filler: {
                        propagate: false
                    },
                    'samples-filler-analyser': {
                        target: 'chart-analyser'
                    }
                }

            }
        };

        var xLabels = getXLabel(counters);
        

        var lines = {};
        var percentageLines = {};
        labels.forEach(label => {
            lines[label] = [];
            percentageLines[label] = [];
        });

        counters.forEach(counter => {
            var sum = 0.;
            for (var name in lines) {
                if (counter["Counters"][name] == null) {
                    lines[name].push(0);
                }
                else {
                    lines[name].push(counter["Counters"][name]);
                }
                sum += lines[name][lines[name].length - 1];
            }

            for (var name in lines) {
                if (sum == 0) percentageLines[name].push(0);
                else percentageLines[name].push(lines[name][lines[name].length - 1] / sum);
            }

        });



        var datasets = [];
        var i = 0;
        for (var name in percentageLines) {
            var dataset = {
                backgroundColor: transparentize(backgroundColor[i]),
                borderColor: backgroundColor[i],
                data: percentageLines[name],
                label: name,
                fill: name == "message:lt:100" ? true : '-1'
            };
            datasets.push(dataset);
            i++;
        }

        lineConfig.data.datasets = datasets;
        lineConfig.data.labels = xLabels;

        var ctx = document.getElementById(`chart-area-${scenario}-line-percentage`).getContext('2d');
        var lineChart = new Chart(ctx, lineConfig);

    }
}

function createMessageRateLineChart(results) {
    // line chart
    var lineCharts = [];
    for (var scenario in results) {
        var counters = results[scenario];

        var xLabels = getXLabel(counters);

        var lines = {};
        var messageRateLines = { "message:sent": [], "message:received": [] };
        labels.forEach(label => {
            lines[label] = [];
        });

        counters.forEach(counter => {
            messageRateLines["message:received"].push(counter["Counters"]["message:received"] == null ? 0 : counter["Counters"]["message:received"]);
            messageRateLines["message:sent"].push(counter["Counters"]["message:sent"] == null ? 0 : counter["Counters"]["message:sent"]);
        });

        for (var i = 0; i < messageRateLines["message:sent"].length; i++) {
            if (i + 1 == messageRateLines["message:sent"].length) {
                messageRateLines["message:sent"][i] = 0;
                messageRateLines["message:received"][i] = 0;
            } else {
                messageRateLines["message:sent"][i] = messageRateLines["message:sent"][i + 1] - messageRateLines["message:sent"][i];
                messageRateLines["message:received"][i] = messageRateLines["message:received"][i + 1] - messageRateLines["message:received"][i];
            }
        }


        var msgRateLineConfig = {
            type: "line",
            data: {
                labels: [],
                datasets: []
            },
            options: {
                responsive: true,
                legend: {
                    position: 'right'
                },
                title: {
                    display: true,
                    text: 'Message Rate In Time'
                },
                elements: {
                    line: {
                        tension: 0.000001
                    },
                    point: {
                        radius: 0
                    }
                },
                scales: {
                    yAxes: [{
                        stacked: false
                    }]
                },
                plugins: {
                    filler: {
                        propagate: false
                    },
                    'samples-filler-analyser': {
                        target: 'chart-analyser'
                    }
                }

            }
        };

        var msgRateDatasets = [];
        var i = 0;
        for (var name in messageRateLines) {
            var msgRateDataset = {
                backgroundColor: transparentize(backgroundColor[i]),
                borderColor: backgroundColor[i],
                data: messageRateLines[name].slice(0, -1),
                label: name,
                fill: 'false'
            };
            msgRateDatasets.push(msgRateDataset);
            i++;
        }
        msgRateLineConfig.data.datasets = msgRateDatasets;
        msgRateLineConfig.data.labels = xLabels.slice(0, -1);

        var ctx = document.getElementById(`chart-area-${scenario}-line-messageRate`).getContext('2d');
        var msgRateLineChart = new Chart(ctx, msgRateLineConfig);


    }
}

function createLatencyDistributionTable(counters) {
    
    var headCell = name => `<th scope="col"> ${name} </th>`;
    var headCol = "";
    headCol += headCell("Latency");
    labels.forEach(l => headCol += headCell(l.split(":").slice(-2).join(" ")));
    headCol += headCell("not received");
    
    var rowCell = val => `<td>${val}</td>`; 
    var row = "";
    row += rowCell("%");
    var lastLine =  counters[counters.length - 1]["Counters"];
    var sum = 0;
    sum = lastLine["message:sent"];
    var percentageLine = labels.map(l => (lastLine[l] || 0) / sum * 100);
    percentageLine.forEach(l => row += rowCell(Number.parseFloat(l).toFixed(1)));
    row += rowCell(Number.parseFloat((lastLine["message:sent"] - lastLine["message:received"])/sum*100).toFixed(1));
    var rows = `<tr>${row}</tr>`;

    var head = `<thead class="thead-striped"><tr>${headCol}</tr></thead>`;
    var body = `<tbody>${rows}</tbody>`;
    var table = `<table class="table">${head}${body}</table>`;
    return table;
}