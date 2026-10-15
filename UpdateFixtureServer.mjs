import http from 'node:http';import fs from 'node:fs';
const files={'/api/public/release':'server/public/release.json','/downloads/AsanDownload-Update.exe':'server/public/downloads/AsanDownload-Update.exe'};
http.createServer((request,response)=>{const file=files[request.url];if(!file){response.writeHead(404);response.end();return;}const bytes=fs.readFileSync(file);response.writeHead(200,{'Content-Length':bytes.length,'Content-Type':file.endsWith('.json')?'application/json':'application/octet-stream'});response.end(bytes);}).listen(18745,'127.0.0.1');
