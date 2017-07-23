// content of index.js
const http = require('http')  
const port = 8080

const requestHandler = (request, response) => {  
  console.log(request.headers);
  console.log(request.url)
  response.end('Hello Node.js Server!')
}

const server = http.createServer(requestHandler)

server.listen(port, (err) => {  
  if (err) {
    return console.log('something bad happened', err)
  }

  console.log(`server is listening on ${port}`)
})