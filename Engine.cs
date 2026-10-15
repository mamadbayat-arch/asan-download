using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace MehrDownload {
 public class DownloadState {
  public string Url, FinalUrl, Target, Validator;
  public long Size;
  public bool Ranges;
  public int Connections;
  public int PartCount;
  public long ChunkSize;
  public double ActiveSeconds;
  public long TimedBytes;
  public bool HasTiming;
 }
 public class DownloadEngine {
  public DownloadState State;
  public string Status = "آماده";
  public long Received;
  public long SessionReceived;
  public int ActiveConnections;
  public volatile bool Running;
  public string Error;
  CancellationTokenSource cancel;
  readonly object gate = new object();
  readonly List<HttpWebRequest> requests = new List<HttpWebRequest>();
  public static HttpWebRequest Request(string url) {
   var r = (HttpWebRequest)WebRequest.Create(url);
   r.UserAgent = "MehrDownload/1.0";
   r.Timeout = 20000; r.ReadWriteTimeout = 20000;
   r.AutomaticDecompression = DecompressionMethods.None;
   r.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
   r.AllowAutoRedirect = true;
   r.ServicePoint.ConnectionLimit=32;
   return r;
  }
  public static string FileName(string url) {
   string name = Uri.UnescapeDataString(new Uri(url).AbsolutePath.Split('/').Last());
   foreach(char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
   name = name.Trim().TrimEnd('.');
   if (String.IsNullOrEmpty(name)) name = "download.bin";
   string stem = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
   if(new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","LPT1","LPT2","LPT3"}.Contains(stem)) name = "file_" + name;
   return name;
  }
  string Parts { get { return State.Target + ".mehrparts"; } }
  string Part(int i) { return Path.Combine(Parts, i + ".part"); }
  int Count {get{return State.PartCount>0?State.PartCount:State.Connections;}}
  long Start(int i) { return State.ChunkSize>0?State.ChunkSize*i:State.Size*i/Count; }
  long Length(int i) { return State.ChunkSize>0?Math.Min(State.ChunkSize,State.Size-Start(i)):State.Size*(i+1)/Count-Start(i); }
  void Track(HttpWebRequest r) { lock(gate) requests.Add(r); }
  void Untrack(HttpWebRequest r) { lock(gate) requests.Remove(r); }
  public void Pause() {
   lock(gate) { if(cancel != null) cancel.Cancel(); foreach(var r in requests.ToArray()) r.Abort(); }
  }
  public Task Run(string url, string target, int connections) {
   if(Running) throw new InvalidOperationException("دانلود در حال اجراست");
   Running=true;Error=null;State=null;Received=0;SessionReceived=0;ActiveConnections=0;cancel=new CancellationTokenSource();
   var sessionClock=Stopwatch.StartNew();
   return Task.Run(() => {
    FileStream lease=null;
    try {
     Status="بررسی لینک…";
     Uri uri;
     if(!Uri.TryCreate(url,UriKind.Absolute,out uri) || (uri.Scheme!="http" && uri.Scheme!="https")) throw new Exception("لینک باید با http یا https شروع شود.");
     if(File.Exists(target)) throw new Exception("فایلی با این نام وجود دارد؛ نام دیگری انتخاب کنید.");
     lease=new FileStream(target+".mehrlock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose);
     var probe=Request(url); probe.AddRange(0,0); Track(probe);
     long size; bool ranges; string validator, finalUrl;
     try { using(var response=(HttpWebResponse)probe.GetResponse()) {
      finalUrl=response.ResponseUri.AbsoluteUri;
      ranges=response.StatusCode==HttpStatusCode.PartialContent;
      size=response.ContentLength;
      if(ranges) {
       string cr=response.Headers["Content-Range"] ?? "";
       if(!cr.StartsWith("bytes 0-0/") || !long.TryParse(cr.Substring(10),out size) || size<1) throw new Exception("پاسخ بخش‌بندی سرور معتبر نیست.");
      }
      validator=response.Headers["ETag"];
      if(String.IsNullOrEmpty(validator) || validator.StartsWith("W/")) validator=response.Headers["Last-Modified"];
     }} finally { Untrack(probe); }
     cancel.Token.ThrowIfCancellationRequested();
     State=new DownloadState {Url=url,FinalUrl=finalUrl,Target=target,Size=size,Ranges=ranges,Validator=validator,HasTiming=true,Connections=ranges && size>1048576 ? Math.Max(1,Math.Min(16,connections)) : 1};
     Directory.CreateDirectory(Parts);
     string meta=Path.Combine(Parts,"state.json");
     var json=new JavaScriptSerializer();
     bool reuse=false;
     if(File.Exists(meta)) {
      var old=json.Deserialize<DownloadState>(File.ReadAllText(meta));
      reuse=old!=null&&old.Url==url&&old.Size==size&&old.Validator==validator&&ranges&&!String.IsNullOrEmpty(validator);
      if(reuse){State.PartCount=old.PartCount>0?old.PartCount:old.Connections;State.ChunkSize=old.ChunkSize;State.ActiveSeconds=old.ActiveSeconds;State.TimedBytes=old.TimedBytes;State.HasTiming=old.HasTiming;}
      else {
       foreach(var f in Directory.GetFiles(Parts,"*.part")) File.Delete(f);
      }
     } else foreach(var f in Directory.GetFiles(Parts,"*.part")) File.Delete(f);
     if(!reuse){State.PartCount=State.Connections;if(ranges&&size>64L*1048576){State.ChunkSize=Math.Max(8L*1048576,((size+2047)/2048+1048575)/1048576*1048576);State.PartCount=(int)((size+State.ChunkSize-1)/State.ChunkSize);}}
     File.WriteAllText(meta,json.Serialize(State));
     Received=0;
     for(int i=0;i<Count;i++) {
      if(File.Exists(Part(i)) && ranges && new FileInfo(Part(i)).Length>Length(i)) File.Delete(Part(i));
      if(File.Exists(Part(i))) Received+=new FileInfo(Part(i)).Length;
     }
     Status=ranges ? "در حال دانلود • " + State.Connections + " اتصال" : "در حال دانلود • اتصال تکی";
     var tasks=new List<Task>();
     int next=-1;
     int minimumWorkers,minimumIo;ThreadPool.GetMinThreads(out minimumWorkers,out minimumIo);ThreadPool.SetMinThreads(Math.Max(minimumWorkers,State.Connections+2),minimumIo);
     for(int i=0;i<Math.Min(State.Connections,Count);i++)tasks.Add(Task.Run(()=>{int n;while((n=Interlocked.Increment(ref next))<Count)Fetch(n,cancel.Token);}));
     try { Task.WaitAll(tasks.ToArray()); } catch { Pause(); throw; }
     cancel.Token.ThrowIfCancellationRequested();
     Status="یکپارچه‌سازی فایل…";
     string assembling=Path.Combine(Parts,"assembled.tmp");
     using(var output=new FileStream(assembling,FileMode.Create,FileAccess.Write,FileShare.None,1048576,FileOptions.SequentialScan)) {
      for(int i=0;i<Count;i++) using(var input=File.OpenRead(Part(i))) {
       var b=new byte[1048576]; int read;
       while((read=input.Read(b,0,b.Length))>0) { cancel.Token.ThrowIfCancellationRequested(); output.Write(b,0,read); }
      }
      if(size>=0 && output.Length!=size) throw new Exception("حجم فایل دریافت‌شده با حجم سرور مطابقت ندارد.");
     }
     File.Move(assembling,target);
     Directory.Delete(Parts,true);
     Status="دانلود کامل شد";
    } catch(Exception ex) {
     if(cancel.IsCancellationRequested && Error==null) Status="متوقف شد؛ آمادهٔ ادامه";
     else { Error=ex.GetBaseException().Message; Status="دانلود ناموفق؛ امکان تلاش دوباره"; }
    } finally {
     sessionClock.Stop();
     if(State!=null){State.ActiveSeconds+=sessionClock.Elapsed.TotalSeconds;State.TimedBytes+=Interlocked.Read(ref SessionReceived);
      if(Directory.Exists(Parts))try{string metadata=Path.Combine(Parts,"state.json"),temp=metadata+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(State));if(File.Exists(metadata))File.Replace(temp,metadata,null);else File.Move(temp,metadata);}catch{State.HasTiming=false;}
     }
     if(lease!=null)lease.Dispose();Running=false;
    }
   });
  }
  void Fetch(int i,CancellationToken token) {
   for(int attempt=0;attempt<4;attempt++) {
    token.ThrowIfCancellationRequested();
    long existing=File.Exists(Part(i)) ? new FileInfo(Part(i)).Length : 0;
    if(State.Ranges && existing==Length(i)) return;
    if(!State.Ranges && existing>0) { File.Delete(Part(i)); Interlocked.Add(ref Received,-existing); existing=0; }
    var r=Request(State.FinalUrl);
    if(State.Ranges) { r.AddRange(Start(i)+existing,Start(i)+Length(i)-1); if(!String.IsNullOrEmpty(State.Validator)) r.Headers["If-Range"]=State.Validator; }
    Track(r);
    bool active=false;
    try {
     using(var response=(HttpWebResponse)r.GetResponse()) {
      if(State.Ranges) {
       string expected="bytes " + (Start(i)+existing) + "-" + (Start(i)+Length(i)-1) + "/" + State.Size;
       if(response.StatusCode!=HttpStatusCode.PartialContent || response.Headers["Content-Range"]!=expected) throw new InvalidDataException("سرور ادامهٔ دانلود را تأیید نکرد؛ لینک یا فایل تغییر کرده است.");
      }
      Interlocked.Increment(ref ActiveConnections);active=true;
      using(var stream=response.GetResponseStream()) using(var file=new FileStream(Part(i),FileMode.Append,FileAccess.Write,FileShare.Read,1048576,FileOptions.SequentialScan)) {
       byte[] buffer=new byte[262144]; int read;
       while((read=stream.Read(buffer,0,buffer.Length))>0) {
        token.ThrowIfCancellationRequested();
        if(State.Ranges && file.Length+read>Length(i)) throw new InvalidDataException("حجم بخش دانلود از مقدار مجاز بیشتر است.");
        file.Write(buffer,0,read);Interlocked.Add(ref Received,read);Interlocked.Add(ref SessionReceived,read);
       }
       if(State.Ranges && file.Length!=Length(i)) throw new IOException("ارتباط پیش از تکمیل دانلود قطع شد.");
      }
     }
     return;
    } catch(Exception ex) {
     token.ThrowIfCancellationRequested();
     if(attempt==3 || ex is InvalidDataException) { Error=ex.Message; Pause(); throw; }
     if(token.WaitHandle.WaitOne(800*(attempt+1))) token.ThrowIfCancellationRequested();
    } finally {if(active)Interlocked.Decrement(ref ActiveConnections);Untrack(r);}
   }
  }
 }
}
