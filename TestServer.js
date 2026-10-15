const http = require('http');
const data = Buffer.alloc(4 * 1024 * 1024 + 137);
for (let i = 0; i < data.length; i++) data[i] = (i * 31 + Math.floor(i / 997)) % 256;
let hits = [], fails = 0, version = 1;
http.createServer((req, res) => {
  if (req.url === '/stats') return res.end(JSON.stringify(hits));
  if (req.url === '/change') { version++; return res.end('ok'); }
  if (req.url === '/redirect') { res.writeHead(302, { Location: '/range' }); return res.end(); }
  if (req.url === '/missing') { res.writeHead(404); return res.end(); }
  hits.push({url: req.url, range: req.headers.range || ''});
  const range = /^bytes=(\d+)-(\d+)$/.exec(req.headers.range || '');
  const probe = range && range[1] === '0' && range[2] === '0';
  if (req.url === '/retry' && !probe && fails++ < 2) { res.writeHead(503); return res.end(); }
  if (req.url === '/zero') { res.writeHead(200, {'Content-Length': 0}); return res.end(); }
  let body = data, status = 200, headers = {'ETag': '"v' + version + '"'};
  if (range && req.url !== '/plain' && req.url !== '/unknown' && !(req.url === '/bad' && !probe)) {
    const from = Number(range[1]), to = Number(range[2]);
    body = data.subarray(from, to + 1); status = 206;
    headers['Content-Range'] = `bytes ${from}-${to}/${data.length}`;
  }
  if(req.url === '/novalidator') delete headers.ETag;
  if (req.url !== '/unknown') headers['Content-Length'] = body.length;
  res.writeHead(status, headers);
  let offset = 0;
  const send = () => {
    if (res.destroyed) return;
    if (offset >= body.length) return res.end();
    res.write(body.subarray(offset, offset + 32768)); offset += 32768;
    setTimeout(send, probe ? 0 : 4);
  };
  send();
}).listen(18743, '127.0.0.1');
