import http from 'node:http';
const size=256*1048576,chunk=Buffer.alloc(131072,109);let active=0,maxActive=0,requests=0;
http.createServer((req,res)=>{
 if(req.url==='/stats'){res.end(JSON.stringify({active,maxActive,requests}));return;}
 const range=/^bytes=(\d+)-(\d+)$/.exec(req.headers.range||'');const from=range?Number(range[1]):0,to=range?Number(range[2]):size-1,probe=from===0&&to===0;
 res.writeHead(range?206:200,{'Content-Length':to-from+1,ETag:'"adaptive-v1"',...(range?{'Content-Range':`bytes ${from}-${to}/${size}`}:{})});
 if(probe){res.end(Buffer.from([109]));return;}
 requests++;active++;maxActive=Math.max(active,maxActive);let remaining=to-from+1;res.on('close',()=>active--);
 const send=()=>{if(res.destroyed)return;if(!remaining){res.end();return;}const bytes=Math.min(chunk.length,remaining);remaining-=bytes;res.write(chunk.subarray(0,bytes));setTimeout(send,from===0?32:5);};send();
}).listen(18746,'127.0.0.1',()=>console.log('Adaptive fixture ready'));
