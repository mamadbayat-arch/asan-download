// Only physical clicks/keyboard activation can authorize automatic interception.
// Signals stay in this browser session and expire in 15 seconds.
function markDownloadIntent(event){
 if(!event.isTrusted)return;
 if(event.type==='keydown'&&!['Enter',' '].includes(event.key))return;
 const control=event.target.closest?.('a,button,input[type="submit"],input[type="button"],[role="button"]');
 if(!control&&event.type!=='submit')return;
 if(event.type==='click'&&event.button!==0)return;
 const href=control?.matches('a')?control.getAttribute('href'):null;
 const linkUrl=href&&!href.startsWith('#')?control.href:null;
 chrome.runtime.sendMessage({action:'download-intent',linkUrl}).catch(()=>{});
}
for(const type of ['click','keydown','submit'])document.addEventListener(type,markDownloadIntent,true);
