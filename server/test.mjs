import assert from 'node:assert/strict';
import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import {spawn} from 'node:child_process';import {fileURLToPath} from 'node:url';
const home=path.dirname(fileURLToPath(import.meta.url)),dataDir=fs.mkdtempSync(path.join(home,'test-data-')),origin='https://downloader.hamtavar.ir',base='http://127.0.0.1:4088',password=crypto.randomBytes(24).toString('hex'),salt=crypto.randomBytes(32).toString('hex');
fs.writeFileSync(path.join(dataDir,'admin.json'),JSON.stringify({username:'admin',salt,hash:crypto.scryptSync(password,salt,64).toString('hex')}),{mode:0o600});
const server=spawn(process.execPath,[path.join(home,'server.mjs')],{env:{...process.env,PORT:'4088',HOST:'127.0.0.1',PUBLIC_ORIGIN:origin,NODE_ENV:'production',DATA_DIR:dataDir},stdio:['ignore','pipe','pipe']});let log='';server.stdout.on('data',v=>log+=v);server.stderr.on('data',v=>log+=v);
try{
 for(let n=0;n<60&&!log.includes('server ready');n++){if(server.exitCode!==null)throw Error(log);await new Promise(r=>setTimeout(r,50));}assert.ok(log.includes('server ready'));
 const call=(route,options={})=>fetch(base+route,options),report={id:'1'.repeat(32),category:'ظاهر برنامه',title:'تراز دکمه‌ها',description:'گزارش آزمایشی؛ اطلاعات کاربر واقعی نیست.',contact:'',version:'1.6.1'};
 assert.equal((await call('/')).status,200);assert.equal((await call('/admin/reports')).status,200);assert.equal((await call('/api/admin/reports')).status,401);
 const publicHeaders={'Content-Type':'application/json'};
 assert.equal((await call('/api/public/reports',{method:'POST',headers:publicHeaders,body:JSON.stringify({...report,title:''})})).status,400);
 assert.equal((await call('/api/public/reports',{method:'POST',headers:publicHeaders,body:JSON.stringify(report)})).status,201);
 assert.equal((await call('/api/public/reports',{method:'POST',headers:publicHeaders,body:JSON.stringify(report)})).status,202);
 const login=await call('/api/admin/login',{method:'POST',headers:{...publicHeaders,Origin:origin},body:JSON.stringify({username:'admin',password})});assert.equal(login.status,200);assert.ok(login.headers.get('Set-Cookie').includes('Secure'));
 const cookie=login.headers.get('Set-Cookie').split(';')[0],csrf=(await login.json()).csrf,headers={...publicHeaders,Cookie:cookie,Origin:origin,'X-CSRF-Token':csrf};
 let listing=await(await call('/api/admin/reports',{headers})).json();assert.equal(listing.items.length,1);assert.equal(listing.items[0].title,report.title);
 const update='/api/admin/reports/'+report.id,changes=JSON.stringify({status:'resolved',notes:'اصلاح شد'});
 assert.equal((await call(update,{method:'PATCH',headers:{...publicHeaders,Cookie:cookie},body:changes})).status,403);assert.equal((await call(update,{method:'PATCH',headers,body:changes})).status,200);
 listing=await(await call('/api/admin/reports',{headers})).json();assert.equal(listing.items[0].status,'resolved');assert.equal(JSON.parse(fs.readFileSync(path.join(dataDir,'reports',report.id+'.json'))).notes,'اصلاح شد');
 assert.equal((await call('/data/admin.json')).status,404);assert.equal((await call('/reports/'+report.id+'.json')).status,404);
 const release=await(await call('/api/public/release')).json(),manifest=path.join(home,'public','release.json');
 if(fs.existsSync(manifest)){
  const built=JSON.parse(fs.readFileSync(manifest,'utf8'));assert.equal(release.version,built.version);const raw=Buffer.from(await(await call(release.updateUrl)).arrayBuffer());assert.equal(raw.length,release.exeSize);assert.equal(crypto.createHash('sha256').update(raw).digest('hex'),release.sha256);
  const range=await call(release.downloadUrl,{headers:{Range:'bytes=0-63'}});assert.equal(range.status,206);assert.equal((await range.arrayBuffer()).byteLength,64);
 }else assert.deepEqual(release,{available:false});
 console.log('PASS: landing, reports, login, validation, idempotency, CSRF, persistence and release metadata; built releases also verify hashes and byte ranges.');
}finally{server.kill('SIGTERM');await new Promise(resolve=>server.exitCode!==null?resolve():server.once('exit',resolve));fs.rmSync(dataDir,{recursive:true,force:true});}
