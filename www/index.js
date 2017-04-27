var express = require("express");
var app = express();

if (process.argv.indexOf("--jitter") != -1) {
  console.log("Applying jitter to requests");
  app.use((req,res,next) => {
    setTimeout(next, Math.random() * 1000);
  });
}

app.use(express.static(__dirname))

app.listen(8080, () => {
  console.log('Serving GLTF Assets on port 8080');
});