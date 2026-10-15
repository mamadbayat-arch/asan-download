// Download the corresponding source in the browser without handing the
// download manager's own distribution back to a browser integration host.
for(const link of document.querySelectorAll('[data-browser-download]'))link.addEventListener('click',async event=>{
 event.preventDefault();const status=document.getElementById('download-status');status.textContent='در حال آماده‌سازی فایل سورس…';
 try{const result=await fetch(link.href);if(!result.ok)throw Error('HTTP '+result.status);const url=URL.createObjectURL(await result.blob()),anchor=document.createElement('a');anchor.href=url;anchor.download='AsanDownload-Gopeed-2.0.1-Source.zip';anchor.click();setTimeout(()=>URL.revokeObjectURL(url),60000);status.textContent='فایل سورس برای دانلود آماده شد.';}
 catch{status.textContent='دریافت فایل انجام نشد؛ کمی بعد دوباره تلاش کنید.';}
});
