using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using MehrDownload;
class Tests {
 static string root;
 static void Check(bool ok,string name) {if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
 static void Valid(string file) {
  byte[] data=File.ReadAllBytes(file);Check(data.Length==4*1024*1024+137,"exact length");
  for(int i=0;i<data.Length;i++)if(data[i]!=(byte)((i*31+i/997)%256))throw new Exception("Corrupt byte: "+i);
  Console.WriteLine("PASS: every byte matches");
 }
 static void Main() {
  ServicePointManager.DefaultConnectionLimit=32;
  root=Path.Combine(Environment.CurrentDirectory,"test-output-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  foreach(string route in new[]{"range","plain","redirect","unknown","retry"}) {
   var e=new DownloadEngine();string path=Path.Combine(root,route+".bin");e.Run("http://127.0.0.1:18743/"+route,path,8).Wait();
   Check(e.Error==null && File.Exists(path),route);Valid(path);
   Check(e.State.Connections==(route=="plain" || route=="unknown" ? 1 : 8),"connection selection");
  }
  var paused=new DownloadEngine();string p=Path.Combine(root,"resume.bin");var job=paused.Run("http://127.0.0.1:18743/range",p,8);
  while(paused.Received<262144 && paused.Running)Thread.Sleep(10);paused.Pause();job.Wait();
  Check(!File.Exists(p) && paused.Error==null,"pause keeps incomplete file private");
  double activeBeforePause=paused.State.ActiveSeconds;Check(activeBeforePause>0&&paused.State.HasTiming,"active duration recorded before pause");
  long stored=Directory.GetFiles(p+".mehrparts","*.part").Sum(f=>new FileInfo(f).Length);Check(stored>0,"parts persisted");
  var resumed=new DownloadEngine();resumed.Run("http://127.0.0.1:18743/range",p,8).Wait();Check(resumed.Error==null,"resume in new engine instance");Check(resumed.State.HasTiming&&resumed.State.ActiveSeconds>activeBeforePause&&resumed.State.TimedBytes==resumed.State.Size,"duration and transfer totals continue across engine instances");Valid(p);
  var bad=new DownloadEngine();string b=Path.Combine(root,"bad.bin");bad.Run("http://127.0.0.1:18743/bad",b,8).Wait();Check(bad.Error!=null && !File.Exists(b),"reject ignored range without publishing corruption");
  var zero=new DownloadEngine();string z=Path.Combine(root,"zero.bin");zero.Run("http://127.0.0.1:18743/zero",z,8).Wait();Check(zero.Error==null && new FileInfo(z).Length==0,"zero-byte file");
  var missing=new DownloadEngine();missing.Run("http://127.0.0.1:18743/missing",Path.Combine(root,"missing.bin"),8).Wait();Check(missing.Error!=null,"404 handled");
  var duplicate=new DownloadEngine();duplicate.Run("http://127.0.0.1:18743/range",p,8).Wait();Check(duplicate.Error!=null,"existing destination protected");Valid(p);
  foreach(string route in new[]{"range","novalidator"}) {
   string changed=Path.Combine(root,route+"-changed.bin");var first=new DownloadEngine();var run=first.Run("http://127.0.0.1:18743/"+route,changed,8);
   while(first.Received<262144 && first.Running)Thread.Sleep(10);first.Pause();run.Wait();
   foreach(string f in Directory.GetFiles(changed+".mehrparts","*.part"))using(var fs=File.OpenWrite(f)){fs.WriteByte(99);}
   using(var wc=new WebClient())wc.DownloadString("http://127.0.0.1:18743/change");
   var next=new DownloadEngine();next.Run("http://127.0.0.1:18743/"+route,changed,8).Wait();Check(next.Error==null,"safe reset: "+route);Valid(changed);
  }
  Console.WriteLine("ALL TESTS PASSED. Artifacts: "+root);
 }
}
