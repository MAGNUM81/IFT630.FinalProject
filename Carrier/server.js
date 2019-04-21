const express = require('express')
const http = require("http");
const bodyParser = require('body-parser')
const app = express()
const port = 8082

app.use(bodyParser.urlencoded({
    extended: true
}))

app.use(bodyParser.json())

app.post('/ToBOMWarehouse', (req, res) => {
    console.log('Got a POST request, responding and forwarding content to BOMWarehouse')
    res.statusCode = 200;
    res.setHeader('Content-Type', 'application/json');
    console.log(`${req.body.content}`)
    res.send(req.body);
})

app.post('/ToProductionArea', (req, res) => {
    console.log('Got a POST request, responding and forwarding content to ProductionArea')
    res.statusCode = 200;
    res.setHeader('Content-Type', 'application/json');
    res.send('Hello World\n');
})

app.post('/ToFinalWarehouse', (req, res) => {
    console.log('Got a POST request, responding and forwarding to FinalWarehouse!')
    res.send(res)
})

function Post(_host, _path, content){
    var options = {
        host:_host,
        path:_path,
        method:"POST",
        headers:{
            "Content-Type":"application/json"
        }
    }
    
    var req = http.request(options, function(res){
        var responseString = "";
        
        res.on("data", function(data){
            responseString += data;
        });
        res.on("end", function(){
           console.log(responseString); 
        });
    });
    
    req.write(content);
    req.end();
}

app.listen(port, () => console.log(`Example app listening on port ${port}!`))