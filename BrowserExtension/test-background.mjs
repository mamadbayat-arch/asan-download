import fs from 'node:fs';import vm from 'node:vm';import assert from 'node:assert/strict';
const source=fs.readFileSync(new URL('./background.js',import.meta.url),'utf8');
function worker(reply,{failed=false,session={}}={}){
 let handler,messageHandler;const calls=[],chrome={storage:{local:{get:async()=>({enabled:true}),set:async()=>{}},session:{get:async defaults=>({...defaults,...session}),set:async values=>Object.assign(session,values)}},contextMenus:{create(){},onClicked:{addListener(){}}},runtime:{onMessage:{addListener(fn){messageHandler=fn;}},onInstalled:{addListener(){}},connectNative(){let message,disconnect;return{onMessage:{addListener(fn){message=fn;}},onDisconnect:{addListener(fn){disconnect=fn;}},postMessage(){calls.push('deliver');queueMicrotask(()=>failed?disconnect():message(reply));},disconnect(){}};}},downloads:{onCreated:{addListener(fn){handler=fn;}},pause:async()=>calls.push('pause'),cancel:async()=>calls.push('cancel'),resume:async()=>calls.push('resume')}};
 vm.runInNewContext(source,{chrome,Promise,Error,setTimeout,clearTimeout,Date,URL});
 return {calls,session,click:async(linkUrl='https://example.com/file.zip')=>new Promise(resolve=>messageHandler({action:'download-intent',linkUrl},{tab:{id:1},url:'https://example.com/page'},resolve)),download:async(item={})=>handler({id:1,url:'https://example.com/file.zip',referrer:'https://example.com/page',startTime:new Date().toISOString(),state:'in_progress',danger:'safe',...item})};
}
async function scenario(reply,{failed=false,item={},click=true,linkUrl='https://example.com/file.zip'}={}){const w=worker(reply,{failed});if(click)await w.click(linkUrl);await w.download(item);return w.calls;}
assert.deepEqual(await scenario({accepted:true}),['pause','deliver','cancel']);
assert.deepEqual(await scenario({cancelled:true}),['pause','deliver','cancel']);
assert.deepEqual(await scenario({reason:'browser_required'}),['pause','deliver','resume']);
assert.deepEqual(await scenario(null,{failed:true}),['pause','deliver','resume']);
assert.deepEqual(await scenario({}, {item:{danger:'uncommon'}}),[]);
assert.deepEqual(await scenario({}, {item:{byExtensionId:'other'}}),[]);
assert.deepEqual(await scenario({}, {click:false}),[]);
assert.deepEqual(await scenario({}, {item:{startTime:new Date(Date.now()-3600000).toISOString()}}),[]);
assert.deepEqual(await scenario({}, {item:{url:'https://steamcdn.example.com/SteamSetup.exe'}}),[]);
assert.deepEqual(await scenario({}, {linkUrl:null,item:{url:'https://cdn.example.com/redirected.zip'}}),['pause','deliver','resume']);
assert.deepEqual(await scenario({}, {linkUrl:null,item:{referrer:'https://unrelated.example.com/'}}),[]);
const expired=worker({accepted:true},{session:{recentIntents:[{tab:1,page:'https://example.com/page',link:'https://example.com/file.zip',at:Date.now()-16000}]}});await expired.download();assert.deepEqual(expired.calls,[]);
const first=worker({accepted:true});await first.click();const recycled=worker({accepted:true},{session:first.session});await recycled.download();assert.deepEqual(recycled.calls,['pause','deliver','cancel']);await recycled.download();assert.equal(recycled.calls.length,3);
const reboot=worker({accepted:true});for(let id=1;id<=5;id++)await reboot.download({id,url:'https://example.com/SteamSetup'+id+'.exe'});assert.deepEqual(reboot.calls,[]);
const content=fs.readFileSync(new URL('./intent.js',import.meta.url),'utf8'),listeners={},sent=[];vm.runInNewContext(content,{document:{addEventListener:(type,handler)=>listeners[type]=handler},chrome:{runtime:{sendMessage:message=>{sent.push(message);return Promise.resolve();}}}});
const anchor={matches:()=>true,getAttribute:()=>'/file.zip',href:'https://example.com/file.zip'},target={closest:()=>anchor};listeners.click({type: 'click',isTrusted:false,button:0,target});assert.equal(sent.length,0);listeners.click({type: 'click',isTrusted:true,button:0,target});assert.equal(sent.length,1);listeners.keydown({type: 'keydown',isTrusted:true,key:'a',target});assert.equal(sent.length,1);listeners.keydown({type: 'keydown',isTrusted:true,key:'Enter',target});assert.equal(sent.length,2);
console.log('PASS: browser confirmation, cancellation/fallback, real clicks, redirects, restored old downloads, five startup requests, worker recycle, browser restart, duplicate events, expired gestures and untrusted script clicks.');
