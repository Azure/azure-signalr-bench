var express = require('express');
var router = express.Router();
var fs = require('fs');
var os = require('os');




/* GET users listing. */
router.get('/', function(req, res, next) {
  let timestamp = req.query.timestamp;
  const resFolder = 'public/results/' + timestamp + '/';

  var scenarios = fs.readdirSync(resFolder, 'utf8');
  var results = {};
  scenarios.forEach(scenario => {
    var text = fs.readFileSync(resFolder + scenario + '/counters.txt', 'utf8');
    var counters = []
    var lines = text.split(os.EOL);
    lines.forEach(line => {
      if (line.length <= 1) return;
      var cntr = JSON.parse(line.slice(0, -1));
      counters.push(cntr);
    });
    results[scenario] = counters;
  });
  // console.log(results);
  // var pieConfigs = [];
  // var datasets = [];
  // scenarios.forEach( scenario => {
  //   var pieConfig = {
  //     type: "pie",
  //     data: {
  //       datasets: [{
  //         data: [],
  //         backgroundColor: [],
  //         label: 'Latency'
  //       }],
  //       labels: labels
  //     },
  //     options: {
  //       responsive: true,
  //       legend: {
  //         position: 'right'
  //       },
  //       title: {
  //         display: true,
  //         text: 'title'
  //       },

  //     }
  //   };

  //   var dataset = {data:[], backgroundColor: backgroundColor, label:scenario};
    
  //   var data = [];
  //   var counters = results[scenario];
  //   var counter = counters[counters.length - 1]['Counters'];
  //   console.log(counter);
  //   pieConfig.data.labels.forEach(label => {
  //     data.push(counter[label]);
  //   });
  //   dataset.data = data;
  //   datasets.push(dataset);
  //   pieConfig.data.datasets = datasets;

  //   pieConfigs.push(pieConfig);
  // });
  
  
  res.render('detail', { data: JSON.stringify(results)});

});

module.exports = router;
