var http = require('http');
var WebSocketServer = require('websocket').server;

var ServerHost = '127.0.0.1';
var ServerPort = 8080;

var ws_connections = [];
var activeTurnIndex = 0;

// [前提1] クライアントは2接続
function onRequest(request, response) {
    console.log('onRequest');
    console.log(request.url);

    if (request.url == '/updatePosition' && request.method === 'POST') {
        // HTTP POST で受けて Web socket で返す
        console.log('updatePosition');
        if (ws_connections.length != 2) {
            console.log('count of client is not 2.');
            response.end();
            return;
        }

        var postData = ''; // [前提] body は json
        request.on('data', function (chunk) {
            postData += chunk;
        });
        request.on('end', function () {
            var json = JSON.parse(postData);
            // 位置情報、終了かどうかの受取り
            console.log(json);

            // ターンのスイッチ
            activeTurnIndex = (activeTurnIndex + 1) % 2;

            // 全クライアントに位置情報、終了かどうか、ターン情報をブロードキャスト
            //ws_connection.send("start");
            //...
            response.end();
        });
    } else {
        response.end();
    }
}

function broadcastToEachClient() {

}

function init() {
    console.log('game server started.');
    var plainServer = http.createServer(onRequest).listen(ServerPort, ServerHost);
    var wsServer = new WebSocketServer({httpServer: plainServer});
    wsServer.on('request', function (request) {
        console.log('wsServer.on(request)');
        //console.log('request.origin: ' + request.origin);

        if (ws_connections.length == 2) {
            console.log('count of max user is 2.');
            return;
        }

        var ws_connection = request.accept(null, request.origin);
        ws_connections.push(ws_connection);

        if (ws_connections.length == 2) {
            // 最初に接続したクライアントにターンを渡す
            ws_connections[0].send('active turn');
            ws_connections[1].send('not active turn');
        }

        ws_connection.on('message', function (message) {
            //console.log('connection.message: ' + message.utf8Data);
        });
        ws_connection.on('close', function (reasonCode, description) {
            console.log('connection.close');
        });
        ws_connection.on('error', function (error) {
        });
    });
}

init();
