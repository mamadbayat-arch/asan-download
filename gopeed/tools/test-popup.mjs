import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const dir=new URL('../browser-extension/',import.meta.url);
const html=fs.readFileSync(new URL('popup.html',dir),'utf8');
const manifest=JSON.parse(fs.readFileSync(new URL('manifest.json',dir),'utf8'));
const elements=new Map([...html.matchAll(/id="([^"]+)"/g)].map(([,id])=>[id,{textContent:'',disabled:false,checked:false,handlers:{},addEventListener(event,fn){this.handlers[event]=fn;}}]));
let complete,fail;
const calls=[],changes=[];
const chrome={storage:{local:{get:async defaults=>defaults,set:async data=>changes.push(data)}},runtime:{
  getManifest:()=>manifest,
  sendNativeMessage:(host,data)=>{calls.push({host,data});return new Promise((resolve,reject)=>{complete=resolve;fail=reject;});},
}};
vm.runInNewContext(fs.readFileSync(new URL('popup.js',dir),'utf8'),{chrome,document:{getElementById:id=>{assert.ok(elements.has(id),'Missing popup element '+id);return elements.get(id);}}});
await Promise.resolve();
assert.equal(elements.get('enabled').checked,true);
assert.ok(elements.get('version').textContent.includes(manifest.version.replace(/\d/g,d=>'۰۱۲۳۴۵۶۷۸۹'[d])));
for(const [button,action] of [['connect','ping'],['open-app','open']]){
  const pending=elements.get(button).handlers.click();
  assert.equal(elements.get('connect').disabled,true);
  assert.equal(elements.get('open-app').disabled,true);
  const call=calls.at(-1);assert.equal(call.host,'ir.hamtavar.asandownload2');assert.equal(call.data.action,action);
  complete({accepted:true});await pending;
  assert.equal(elements.get(button).disabled,false);
  assert.match(elements.get('status').textContent,action==='open'?/باز شد/:/اتصال برقرار/);
}
const failed=elements.get('open-app').handlers.click();fail(Error('host unavailable'));await failed;
assert.match(elements.get('status').textContent,/Install.cmd/);
assert.equal(elements.get('connect').disabled,false);
assert.equal(elements.get('open-app').disabled,false);
elements.get('enabled').checked=false;await elements.get('enabled').handlers.change();
assert.equal(changes.at(-1).enabled,false);
console.log('PASS: popup opening action, native-host identity, loading/error recovery, saved preference and version derived from manifest.');
