import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const home = path.dirname(fileURLToPath(import.meta.url));
const publicDir = path.join(home, 'public');
const dataDir = path.resolve(process.env.DATA_DIR || path.join(home, 'data'));
const production = process.env.NODE_ENV === 'production';
const port = Number(process.env.PORT || 4080);
const origin = new URL(process.env.PUBLIC_ORIGIN || `http://localhost:${port}`).origin;
if (production && !origin.startsWith('https://')) throw new Error('PUBLIC_ORIGIN must use HTTPS in production');
fs.mkdirSync(dataDir, { recursive: true });
fs.mkdirSync(path.join(dataDir, 'media'), { recursive: true });
const adFile = path.join(dataDir, 'ad.json');
const credentialsFile = path.join(dataDir, 'admin.json');
const initial = { active: true, title: 'همتوار؛ همراه روزهای منظم‌تر', description: 'مدیریت کارها، دخل‌وخرج و هدف‌ها در یک برنامهٔ فارسی.', ctaText: 'آشنایی با همتوار', link: 'https://hamtavar.ir', imageUrl: '/assets/hamtavar.png', revision: 1 };
if (!fs.existsSync(adFile)) fs.writeFileSync(adFile, JSON.stringify(initial, null, 2));
function readAd() { return JSON.parse(fs.readFileSync(adFile, 'utf8')); }
function writeAd(ad) { fs.writeFileSync(adFile + '.tmp', JSON.stringify(ad, null, 2)); fs.renameSync(adFile + '.tmp', adFile); }
const sessions = new Map(), attempts = new Map();
const reportDir=path.join(dataDir,'reports');fs.mkdirSync(reportDir,{recursive:true});const reportAttempts=new Map();
function validateReport(data){if(!data||typeof data!=='object'||Array.isArray(data)||typeof data.id!=='string'||! /^[a-f0-9]{32}$/.test(data.id))throw bad(400,'گزارش معتبر نیست.');const report={id:data.id};for(const[key,max]of Object.entries({title:120,description:4000,contact:160,version:32,category:30})){if(typeof data[key]!=='string'||data[key].length>max)throw bad(400,'متن گزارش معتبر نیست.');report[key]=data[key].trim();}if(!report.title||!report.description||!['ظاهر برنامه','دانلود','به‌روزرسانی','سایر'].includes(report.category))throw bad(400,'عنوان، توضیح و نوع مشکل لازم است.');return report;}
const types = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.png': 'image/png', '.jpg': 'image/jpeg', '.webp': 'image/webp', '.woff2': 'font/woff2', '.ico': 'image/x-icon', '.zip': 'application/zip', '.exe': 'application/octet-stream' };
function headers(res) {
 res.setHeader('X-Content-Type-Options', 'nosniff');
 res.setHeader('X-Frame-Options', 'DENY');
 res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
 res.setHeader('Content-Security-Policy', "default-src 'self'; img-src 'self' blob:; style-src 'self'; script-src 'self'; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'");
 if (production) res.setHeader('Strict-Transport-Security', 'max-age=31536000');
}
function json(res, status, body) { res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' }); res.end(JSON.stringify(body)); }
function bad(status, message) { const e = new Error(message); e.status = status; return e; }
function body(req, limit) {
 return new Promise((resolve, reject) => { let length = 0, parts = [];
  req.on('data', chunk => { length += chunk.length; if (length > limit) { reject(bad(413, 'فایل یا درخواست بیش از حد بزرگ است.')); parts = []; } else parts.push(chunk); });
  req.on('end', () => length <= limit && resolve(Buffer.concat(parts))); req.on('error', reject);
 });
}
async function jsonBody(req) { if (!(req.headers['content-type'] || '').startsWith('application/json')) throw bad(415, 'درخواست JSON لازم است.'); try { return JSON.parse((await body(req, 16384)).toString('utf8')); } catch (e) { if (e.status) throw e; throw bad(400, 'درخواست معتبر نیست.'); } }
function session(req) {
 const token = /(?:^|;\s*)asan_session=([a-f0-9]{64})(?:;|$)/.exec(req.headers.cookie || '')?.[1];
 const entry = token && sessions.get(token);
 if (!entry || entry.expires < Date.now()) { if (token) sessions.delete(token); throw bad(401, 'ابتدا وارد پنل شوید.'); }
 return { ...entry, token };
}
function mutation(req, entry) {
 if (req.headers.origin !== origin) throw bad(403, 'مبدأ درخواست معتبر نیست.');
 if (req.headers['x-csrf-token'] !== entry.csrf) throw bad(403, 'دوباره وارد پنل شوید.');
}
function cookie(value, age) { return `asan_session=${value}; Path=/; HttpOnly; SameSite=Strict; Max-Age=${age}${production ? '; Secure' : ''}`; }
function validateAd(input) {
 const text = (key, max) => { if (typeof input[key] !== 'string' || input[key].length > max) throw bad(400, 'متن بنر معتبر نیست.'); return input[key].trim(); };
 const title = text('title', 100), description = text('description', 240), ctaText = text('ctaText', 40), link = text('link', 2048), imageUrl = text('imageUrl', 200);
 let url; try { url = new URL(link); } catch { throw bad(400, 'لینک مقصد معتبر نیست.'); }
 if (!['https:', 'http:'].includes(url.protocol) || url.username || url.password) throw bad(400, 'لینک مقصد باید HTTP یا HTTPS باشد.');
 if (imageUrl !== '/assets/hamtavar.png' && !/^\/media\/[a-f0-9]{32}\.(png|jpg|webp)$/.test(imageUrl)) throw bad(400, 'تصویر را از پنل بارگذاری کنید.');
 if (imageUrl.startsWith('/media/') && !fs.existsSync(path.join(dataDir, imageUrl.slice(1)))) throw bad(400, 'تصویر پیدا نشد.');
 if (typeof input.active !== 'boolean' || !title || !ctaText) throw bad(400, 'عنوان و متن دکمه لازم است.');
 return { active: input.active, title, description, ctaText, link, imageUrl, revision: readAd().revision + 1, updatedAt: new Date().toISOString() };
}
function serve(req, res, file, download = false) {
 if (!fs.existsSync(file) || !fs.statSync(file).isFile()) return json(res, 404, { error: 'فایل پیدا نشد.' });
 const stat = fs.statSync(file), etag = `"${stat.size}-${Math.floor(stat.mtimeMs)}"`;
 res.setHeader('Content-Type', types[path.extname(file)] || 'application/octet-stream');
 res.setHeader('Accept-Ranges', 'bytes'); res.setHeader('ETag', etag); res.setHeader('Cache-Control', download ? 'no-cache' : 'public, max-age=300');
 if (download) res.setHeader('Content-Disposition', `attachment; filename="${path.basename(file)}"`);
 if (req.headers['if-none-match'] === etag && !req.headers.range) { res.writeHead(304); return res.end(); }
 let start = 0, end = stat.size - 1, status = 200;
 if (req.headers.range && (!req.headers['if-range'] || req.headers['if-range'] === etag)) {
  const range = /^bytes=(\d+)-(\d*)$/.exec(req.headers.range);
  if (!range || !stat.size || (start = Number(range[1])) >= stat.size || (range[2] && Number(range[2]) < start)) { res.writeHead(416, { 'Content-Range': `bytes */${stat.size}` }); return res.end(); }
  end = range[2] ? Math.min(Number(range[2]), stat.size - 1) : end; status = 206; res.setHeader('Content-Range', `bytes ${start}-${end}/${stat.size}`);
 }
 res.setHeader('Content-Length', stat.size ? end - start + 1 : 0); res.writeHead(status);
 if (req.method === 'HEAD' || !stat.size) return res.end();
 const stream = fs.createReadStream(file, { start, end });stream.on('error', () => res.destroy());res.on('close', () => stream.destroy());stream.pipe(res);
}
const server = http.createServer(async (req, res) => {
 headers(res);
 try {
  const url = new URL(req.url, origin), route = decodeURIComponent(url.pathname);
  if(route==='/api/public/reports'&&req.method==='POST'){
   if(req.headers.origin&&req.headers.origin!==origin)throw bad(403,'مبدأ درخواست معتبر نیست.');const report=validateReport(await jsonBody(req)),file=path.join(reportDir,report.id+'.json');if(fs.existsSync(file))return json(res,202,{ok:true,id:report.id});
   const ip=req.socket.remoteAddress;let rate=reportAttempts.get(ip);if(!rate||rate.until<Date.now())rate={count:0,until:Date.now()+3600000};if(rate.count>=10)throw bad(429,'تعداد گزارش‌ها زیاد است؛ کمی بعد تلاش کنید.');rate.count++;reportAttempts.set(ip,rate);
   Object.assign(report,{createdAt:new Date().toISOString(),status:'new',notes:''});try{fs.writeFileSync(file,JSON.stringify(report),{flag:'wx'});}catch(e){if(e.code!=='EEXIST')throw e;}return json(res,201,{ok:true,id:report.id});
  }
  if (route === '/api/public/ad' && req.method === 'GET') { res.setHeader('Access-Control-Allow-Origin', '*'); return json(res, 200, readAd()); }
  if (route === '/api/public/release' && req.method === 'GET') {
   const file = path.join(publicDir, 'downloads', 'AsanDownload-Windows.zip');
   return json(res, 200, fs.existsSync(path.join(publicDir, 'release.json')) ? JSON.parse(fs.readFileSync(path.join(publicDir, 'release.json'), 'utf8')) : {available:false});
  }
  if (route === '/api/admin/login' && req.method === 'POST') {
   if (req.headers.origin !== origin) throw bad(403, 'مبدأ درخواست معتبر نیست.');
   const ip = req.socket.remoteAddress, record = attempts.get(ip) || { count: 0, until: Date.now() + 15 * 60000 };
   if (record.until < Date.now()) { record.count = 0; record.until = Date.now() + 15 * 60000; }
   if (record.count >= 8) throw bad(429, 'تلاش زیاد؛ ۱۵ دقیقه بعد امتحان کنید.');
   record.count++; attempts.set(ip, record);
   const input = await jsonBody(req);
   if (!fs.existsSync(credentialsFile)) throw bad(503, 'مدیر سرور هنوز تنظیم نشده است.');
   const admin = JSON.parse(fs.readFileSync(credentialsFile, 'utf8'));
   const derived = crypto.scryptSync(typeof input.password === 'string' ? input.password.slice(0, 512) : '', admin.salt, 64);
   const expected = Buffer.from(admin.hash, 'hex');
   if (input.username !== admin.username || expected.length !== derived.length || !crypto.timingSafeEqual(expected, derived)) throw bad(401, 'نام کاربری یا رمز اشتباه است.');
   attempts.delete(ip); const token = crypto.randomBytes(32).toString('hex'), csrf = crypto.randomBytes(24).toString('hex');
   sessions.set(token, { csrf, expires: Date.now() + 8 * 3600000 }); res.setHeader('Set-Cookie', cookie(token, 8 * 3600)); return json(res, 200, { csrf });
  }
  if (route.startsWith('/api/admin/')) {
   const entry = session(req);
   if (route === '/api/admin/session' && req.method === 'GET') return json(res, 200, { csrf: entry.csrf });
   if (route === '/api/admin/ad' && req.method === 'GET') return json(res, 200, readAd());
   if(route==='/api/admin/reports'&&req.method==='GET'){const names=fs.readdirSync(reportDir).filter(n=>/^[a-f0-9]{32}\.json$/.test(n)).sort(),offset=Number(url.searchParams.get('cursor')||0);if(!Number.isSafeInteger(offset)||offset<0)throw bad(400,'صفحه معتبر نیست.');const items=names.slice(offset,offset+100).map(n=>JSON.parse(fs.readFileSync(path.join(reportDir,n),'utf8'))).sort((a,b)=>b.createdAt.localeCompare(a.createdAt));return json(res,200,{items,cursor:names.length>offset+100?String(offset+100):null});}
   mutation(req, entry);
   if(/^\/api\/admin\/reports\/[a-f0-9]{32}$/.test(route)&&req.method==='PATCH'){const file=path.join(reportDir,route.split('/').pop()+'.json');if(!fs.existsSync(file))throw bad(404,'گزارش پیدا نشد.');const input=await jsonBody(req);if(!input||!['new','in_progress','resolved'].includes(input.status)||typeof input.notes!=='string'||input.notes.length>2000)throw bad(400,'وضعیت یا یادداشت معتبر نیست.');const report=JSON.parse(fs.readFileSync(file,'utf8'));Object.assign(report,{status:input.status,notes:input.notes.trim(),updatedAt:new Date().toISOString()});fs.writeFileSync(file+'.tmp',JSON.stringify(report));fs.renameSync(file+'.tmp',file);return json(res,200,report);}
   if (route === '/api/admin/logout' && req.method === 'POST') { sessions.delete(entry.token); res.setHeader('Set-Cookie', cookie('', 0)); return json(res, 200, { ok: true }); }
   if (route === '/api/admin/ad' && req.method === 'PUT') { const ad = validateAd(await jsonBody(req)); writeAd(ad); return json(res, 200, ad); }
   if (route === '/api/admin/banner' && req.method === 'POST') {
    const bytes = await body(req, 4 * 1024 * 1024); let ext;
    if (bytes.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10]))) ext = 'png';
    else if (bytes[0] === 255 && bytes[1] === 216 && bytes[2] === 255) ext = 'jpg';
    else if (bytes.subarray(0,4).toString() === 'RIFF' && bytes.subarray(8,12).toString() === 'WEBP') ext = 'webp';
    else throw bad(415, 'فقط PNG، JPG یا WEBP معتبر بارگذاری کنید.');
    const name = crypto.randomBytes(16).toString('hex') + '.' + ext; fs.writeFileSync(path.join(dataDir, 'media', name), bytes); return json(res, 201, { imageUrl: '/media/' + name });
   }
   throw bad(404, 'مسیر پیدا نشد.');
  }
  if (!['GET', 'HEAD'].includes(req.method)) throw bad(405, 'روش درخواست مجاز نیست.');
  if (/^\/media\/[a-f0-9]{32}\.(png|jpg|webp)$/.test(route)) return serve(req, res, path.join(dataDir, route.slice(1)));
  const mapped = route === '/' ? '/index.html' : route === '/admin' || route === '/admin/' ? '/admin.html' : route==='/admin/reports'||route==='/admin/reports/'?'/reports.html':route;
  const file = path.resolve(publicDir, '.' + mapped);
  if (!file.startsWith(publicDir + path.sep) || mapped.split('/').some(p => p.startsWith('.'))) throw bad(404, 'مسیر پیدا نشد.');
  return serve(req, res, file, mapped.startsWith('/downloads/'));
 } catch (e) { if (!res.headersSent) json(res, e.status || 500, { error: e.status ? e.message : 'خطای سرور؛ دوباره تلاش کنید.' }); else res.destroy(); }
});
server.requestTimeout = 30000;server.headersTimeout = 15000;
const cleanup = setInterval(() => { for (const [key, value] of sessions) if (value.expires < Date.now()) sessions.delete(key); for (const [key, value] of attempts) if (value.until < Date.now()) attempts.delete(key); }, 60000);cleanup.unref();
server.listen(port, process.env.HOST || '127.0.0.1', () => console.log(`Asan Download server ready on ${port}`));
