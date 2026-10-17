// Exercise the actual packaged Windows app with synthetic data only.
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import http from 'node:http';
import crypto from 'node:crypto';
import {spawn} from 'node:child_process';
import {once} from 'node:events';

assert.equal(process.platform,'win32');
const bundle=path.resolve(import.meta.dirname,'../ui/flutter/build/windows/x64/runner/Release');
const profile=await fs.mkdtemp(path.join(os.tmpdir(),'asan-smoke-'));
const downloads=path.join(profile,'downloads');
await fs.mkdir(downloads,{recursive:true});
await fs.mkdir(path.join(profile,'MehrDownload'));
await fs.writeFile(path.join(profile,'MehrDownload/session.json'),JSON.stringify({Folder:downloads,
  Queue:Array.from({length:5},(_,i)=>`https://example.com/SteamSetup${i}.exe`),
  History:[{Path:path.join(downloads,'missing-old.zip'),Size:100,Finished:'2026-03-21T09:00:00Z'}]}));
const env={...process.env,LOCALAPPDATA:profile,ASAN_TEST_MODE:'1'};
const bytes=Buffer.alloc(4*1024*1024+137);
for(let i=0;i<bytes.length;i++)bytes[i]=(i*31+Math.floor(i/997))%256;
const digest=b=>crypto.createHash('sha256').update(b).digest('hex');
const fixture=http.createServer((req,res)=>{
  if(req.url!='/fixture.bin'){res.writeHead(404);res.end();return;}
  const range=/^bytes=(\d+)-(\d*)$/.exec(req.headers.range??'');
  const start=range?Number(range[1]):0,end=range&&range[2]?Math.min(Number(range[2]),bytes.length-1):bytes.length-1;
  res.writeHead(range?206:200,{'Content-Type':'application/octet-stream','Accept-Ranges':'bytes',
    'Content-Length':end-start+1,...(range?{'Content-Range':`bytes ${start}-${end}/${bytes.length}`}:{})});
  res.end(req.method==='HEAD'?undefined:bytes.subarray(start,end+1));
});
fixture.listen(0,'127.0.0.1');await once(fixture,'listening');
let app;
async function stop(){if(!app||app.exitCode!==null)return;const ended=once(app,'exit');app.kill();await ended;}
function rpc(route,body){return new Promise((resolve,reject)=>{
  const data=Buffer.from(JSON.stringify(body));
  const req=http.request({socketPath:'\\\\.\\pipe\\asan_download_host',path:route,method:'POST',
    headers:{'Content-Type':'application/json','Content-Length':data.length},timeout:4000},res=>{
      let output='';res.on('data',chunk=>output+=chunk);res.on('end',()=>{try{resolve(JSON.parse(output));}catch(e){reject(e);}});
    });req.on('error',reject);req.on('timeout',()=>req.destroy(Error('RPC timeout')));req.end(data);
});}
async function api(apiPath,method='GET',data){const result=await rpc('/forward',{path:apiPath,method,data});assert.equal(result.code,0,JSON.stringify(result));return result.data;}
async function until(check,label){const deadline=Date.now()+45000;let last;while(Date.now()<deadline){
  if(app.exitCode!==null)throw Error('App exited '+app.exitCode);
  try{const value=await check();if(value)return value;}catch(e){last=e;}
  await new Promise(resolve=>setTimeout(resolve,200));
}throw Error(label+': '+(last?.message??'timeout'));}
async function launch(){app=spawn(path.join(bundle,'AsanDownload.exe'),['--hidden'],{cwd:bundle,env,windowsHide:true,stdio:'ignore'});
  await until(()=>api('/api/v1/config'),'app startup');}
async function hostPing(){return new Promise((resolve,reject)=>{
  const child=spawn(path.join(bundle,'host.exe'),[],{cwd:bundle,env,windowsHide:true,stdio:['pipe','pipe','ignore']});
  const timer=setTimeout(()=>{child.kill();reject(Error('Native host timeout'));},15000);
  let output=Buffer.alloc(0);child.on('error',e=>{clearTimeout(timer);reject(e);});
  child.stdout.on('data',chunk=>{output=Buffer.concat([output,chunk]);if(output.length>=4&&output.length>=4+output.readUInt32LE()){
    clearTimeout(timer);const reply=JSON.parse(output.subarray(4,4+output.readUInt32LE()).toString());child.kill();resolve(reply);
  }});
  const data=Buffer.from(JSON.stringify({action:'ping'})),header=Buffer.alloc(4);header.writeUInt32LE(data.length);child.stdin.end(Buffer.concat([header,data]));
});}
try{
  await fs.access(path.join(bundle,'asan_engine.dll'));
  await launch();
  assert.equal((await api('/api/v1/tasks')).length,0,'legacy queue must never become new downloads');
  const config=await api('/api/v1/config');
  assert.equal(path.resolve(config.downloadDir),downloads);
  assert.equal(config.autoDeleteMissingFileTasks,false);
  assert.equal(config.extra.autoStartTasks,false);
  await until(async()=>{try{await fs.access(path.join(bundle,'host.exe'));return true;}catch{return false;}},'host extraction');
  assert.equal((await hostPing()).accepted,true);
  const url=`http://127.0.0.1:${fixture.address().port}/fixture.bin`;
  await api('/api/v1/tasks','POST',{req:{url},opts:{path:downloads,extra:{connections:8,autoExtract:false}}});
  const completed=await until(async()=>{const tasks=await api('/api/v1/tasks');return tasks.find(t=>t.status==='done');},'download completion');
  assert.equal(digest(await fs.readFile(path.join(downloads,'fixture.bin'))),digest(bytes));
  assert.ok(completed.progress.used>0&&completed.progress.used<60e9,'duration must contain active time only');
  const frozen=completed.progress.used;
  await new Promise(resolve=>setTimeout(resolve,1200));
  assert.equal((await api('/api/v1/tasks')).find(t=>t.id===completed.id).progress.used,frozen);
  await api('/api/v1/tasks','POST',{req:{url:url.replace('/fixture.bin','/missing')},opts:{path:downloads}});
  const failed=await until(async()=>{const tasks=await api('/api/v1/tasks');return tasks.find(t=>t.status==='error');},'resolve error');
  assert.ok(failed.progress.used>=0&&failed.progress.used<60e9,'resolve error must not report Unix epoch duration');
  await stop();
  await fs.unlink(path.join(downloads,'fixture.bin'));
  await launch();
  const restored=await api('/api/v1/tasks');
  const history=restored.find(t=>t.id===completed.id);
  assert.ok(history,'completed history must survive restart and a missing file');
  assert.equal(history.progress.used,frozen);
  assert.equal(restored.length,2,'startup must not enqueue five legacy URLs');
  console.log('PASS: Windows engine DLL, native host ping, exact downloaded bytes, duration freeze, resolve error, persistent history, missing files and five stale startup links.');
}finally{await stop();fixture.closeAllConnections();fixture.close();}
