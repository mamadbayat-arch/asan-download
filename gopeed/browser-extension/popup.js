const checkbox=document.getElementById('enabled');
chrome.storage.local.get({enabled:true}).then(data=>checkbox.checked=data.enabled);
checkbox.addEventListener('change',()=>chrome.storage.local.set({enabled:checkbox.checked}));
const version=chrome.runtime.getManifest().version.replace(/\d/g,d=>'۰۱۲۳۴۵۶۷۸۹'[d]);
document.getElementById('version').textContent='نسخهٔ افزونه: '+version;
async function connect(action){
 const status=document.getElementById('status'),buttons=[document.getElementById('connect'),document.getElementById('open-app')];
 buttons.forEach(button=>button.disabled=true);
 status.textContent=action==='open'?'در حال بازکردن برنامه…':'در حال بررسی اتصال…';
 try{
  const result=await chrome.runtime.sendNativeMessage('ir.hamtavar.asandownload2',{action});
  status.textContent=result?.accepted?(action==='open'?'آسان دانلود باز شد. روی لینک فایل کلیک کنید و دانلود را تأیید کنید.':'اتصال برقرار است؛ دانلودها پس از تأیید شما به آسان دانلود می‌روند.'):'برنامه پاسخ نداد؛ آسان دانلود را باز کنید و دوباره بررسی کنید.';
 }catch{
  status.textContent='برنامه پیدا نشد؛ ابتدا Install.cmd بستهٔ آسان دانلود ۲ را اجرا کنید، سپس افزونه و صفحهٔ دانلود را تازه‌سازی کنید.';
 }finally{buttons.forEach(button=>button.disabled=false);}
}
document.getElementById('connect').addEventListener('click',()=>connect('ping'));
document.getElementById('open-app').addEventListener('click',()=>connect('open'));
