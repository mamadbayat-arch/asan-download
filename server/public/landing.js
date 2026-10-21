const fa=value=>String(value).replace(/\d/g,d=>'۰۱۲۳۴۵۶۷۸۹'[Number(d)]);
fetch('/api/public/release',{cache:'no-store'}).then(r=>{if(!r.ok)throw Error();return r.json();}).then(release=>{const link=document.getElementById('download-link'),info=document.getElementById('release-info');if(!release.available){link.setAttribute('aria-disabled','true');info.textContent='فایل نسخهٔ جدید در حال آماده‌سازی است.';return;}if(release.downloadUrl==='/downloads/AsanDownload-Windows.zip')link.href=release.downloadUrl;info.textContent=`ویندوز ۱۰ و ۱۱ · نسخهٔ ${fa(release.version)} · ${fa((release.size/1048576).toFixed(1))} مگابایت · همراه افزونه`;if(release.extensionDownloadUrl==='/downloads/AsanDownload-BrowserExtension.zip'){document.getElementById('extension-download-link').href=release.extensionDownloadUrl;document.getElementById('extension-info').textContent=`Chrome و Edge · ${fa((release.extensionSize/1024).toFixed(0))} کیلوبایت · ابتدا برنامه را نصب کنید.`;}}).catch(()=>{});

// Gentle scroll reveal for every FDM styled card.
(function(){
 const targets=document.querySelectorAll('.feature-row>div,.install,.sponsor,.faq details,.hero-copy,.hero-art,.extension-download');
 if(!('IntersectionObserver' in window)){targets.forEach(el=>el.classList.add('in'));return;}
 const observer=new IntersectionObserver(entries=>{
  entries.forEach(entry=>{
   if(!entry.isIntersecting)return;
   const siblings=Array.from(entry.target.parentElement?entry.target.parentElement.children:[]).filter(el=>el.classList.contains('reveal'));
   const index=siblings.indexOf(entry.target);
   entry.target.style.transitionDelay=(index>0?Math.min(index,5)*70:0)+'ms';
   entry.target.classList.add('in');
   observer.unobserve(entry.target);
  });
 },{threshold:.12,rootMargin:'0px 0px -40px'});
 targets.forEach(el=>{el.classList.add('reveal');observer.observe(el);});
 // Safety net: never leave content hidden if the observer never fires.
 setTimeout(()=>targets.forEach(el=>el.classList.add('in')),1200);
})();
