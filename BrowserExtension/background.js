async function enabled(){return (await chrome.storage.local.get({enabled:true})).enabled;}
const intentLifetime=15000;
let recentIntents=[],handledIds=[];
const intentReady=chrome.storage.session.get({recentIntents:[],handledIds:[]}).then(data=>{recentIntents=data.recentIntents;handledIds=data.handledIds;});
function webUrl(value){try{const url=new URL(value);if(!/^https?:$/.test(url.protocol))return null;url.hash='';return url;}catch{return null;}}
chrome.runtime.onMessage.addListener((message,sender,reply)=>{
 if(message?.action!=='download-intent'||!sender.tab||sender.tab.incognito)return;
 const page=webUrl(sender.url),link=webUrl(message.linkUrl);if(!page)return;
 intentReady.then(async()=>{const now=Date.now();recentIntents=recentIntents.filter(intent=>now-intent.at<intentLifetime&&intent.tab!==sender.tab.id);recentIntents.push({tab:sender.tab.id,page:page.href,link:link?.href||null,at:now});await chrome.storage.session.set({recentIntents});reply({ok:true});});return true;
});
async function requestedByUser(item){
 await intentReady;const now=Date.now(),started=Date.parse(item.startTime);if(!Number.isFinite(started)||handledIds.includes(item.id))return false;
 const original=webUrl(item.url),final=webUrl(item.finalUrl),referrer=webUrl(item.referrer);
 const match=recentIntents.find(intent=>now-intent.at<intentLifetime&&started>=intent.at-2000&&started<=now+2000&&(intent.link?(original?.href===intent.link||final?.href===intent.link):referrer?.origin===webUrl(intent.page)?.origin));
 if(!match)return false;handledIds.push(item.id);handledIds=handledIds.slice(-200);await chrome.storage.session.set({handledIds});return true;
}
function deliver(url){return new Promise((resolve,reject)=>{
 const port=chrome.runtime.connectNative('ir.hamtavar.asandownload');let finished=false;
 const timer=setTimeout(()=>finish(null,Error('مهلت پاسخ برنامه تمام شد.')),130000);
 function finish(reply,error){if(finished)return;finished=true;clearTimeout(timer);port.disconnect();error?reject(error):resolve(reply);}
 port.onMessage.addListener(reply=>finish(reply));port.onDisconnect.addListener(()=>finish(null,Error(chrome.runtime.lastError?.message||'ارتباط با برنامه قطع شد.')));port.postMessage({url});
});}
chrome.downloads.onCreated.addListener(async item=>{
 if(!await enabled()||item.byExtensionId||item.incognito||item.state!=='in_progress'||!/^https?:\/\//i.test(item.url)||/\/AsanDownload-(Windows\.zip|Update\.exe)(\?|$)/i.test(item.url))return;
 if(item.danger&&item.danger!=='safe')return;
 // Restored browser downloads and background page requests have no fresh user gesture.
 if(!await requestedByUser(item))return;
 let paused=false;
 try{
  if(!item.paused){await chrome.downloads.pause(item.id);paused=true;}
  const reply=await deliver(item.finalUrl||item.url);
  if(reply?.accepted||reply?.cancelled){await chrome.downloads.cancel(item.id);}else if(paused){await chrome.downloads.resume(item.id);}
  await chrome.storage.local.set({lastTransfer:reply?.accepted?'درخواست تأیید شد و به برنامه فرستاده شد.':reply?.cancelled?'دانلود با انتخاب شما لغو شد.':'این لینک در مرورگر ادامه پیدا می‌کند.'});
 }catch(error){if(paused)try{await chrome.downloads.resume(item.id);}catch{}await chrome.storage.local.set({lastTransfer:'اتصال برقرار نشد؛ دانلود در مرورگر ادامه پیدا می‌کند.'});}
});
chrome.runtime.onInstalled.addListener(()=>{chrome.contextMenus.create({id:'asan-download',title:'دانلود با آسان دانلود',contexts:['link']});chrome.runtime.sendNativeMessage('ir.hamtavar.asandownload',{action:'ping'}).catch(()=>{});});
chrome.contextMenus.onClicked.addListener(async info=>{if(info.menuItemId==='asan-download'&&/^https?:\/\//i.test(info.linkUrl||''))try{await deliver(info.linkUrl);}catch{}});
