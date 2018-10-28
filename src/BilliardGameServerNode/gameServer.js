var http = require('http');
var WebSocketServer = require('websocket').server;

var ServerHost = '127.0.0.1';
var ServerPort = 8080;

var ws_connection = null;

function onRequest(request, response) {
    console.log('onRequest');
    console.log(request.url);

    response.end();
}

function init() {
    console.log('dummy server started.');
    var plainServer = http.createServer(onRequest).listen(ServerPort, ServerHost);
    var wsServer = new WebSocketServer({httpServer: plainServer});
    wsServer.on('request', function (request) {
        console.log('wsServer.on(request)');
        //console.log('request.origin: ' + request.origin);

        ws_connection = request.accept(null, request.origin);
        ws_connection.on('message', function (message) {
            console.log('connection.message: ' + message.utf8Data);
            // 何らかの処理

            // クライアントに返す
            ws_connection.send("message from server.");
        });
        ws_connection.on('close', function (reasonCode, description) {
            console.log('connection.close');
        });
        ws_connection.on('error', function (error) {
        });
    });
}

init();
