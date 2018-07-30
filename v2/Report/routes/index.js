var express = require('express');
var router = express.Router();

/* GET home page. */
router.get('/', function(req, res, next) {
  
  const rootFolder = 'public/results';
  const fs = require('fs');

  const resDir = fs.readdirSync(rootFolder);

  res.render('index', { 
    title: 'SignalR Benchmark Report', 
    results: resDir
  });
});

module.exports = router;
