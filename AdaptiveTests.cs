using System;using System.IO;using System.Threading;using System.Diagnostics;using System.Web.Script.Serialization;using System.Security.Cryptography;using MehrDownload;
class AdaptiveTests {
 static void Check(bool test,string text){if(!test)throw new Exception(text);Console.WriteLine("PASS: "+text);}
 static string Hash(string file){using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))return Convert.ToBase64String(sha.ComputeHash(stream));}
 static void Main(){
  string root=Path.Combine(Environment.CurrentDirectory,"adaptive-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);string url="http://127.0.0.1:18746/file.bin",old=Path.Combine(root,"fixed.bin"),fresh=Path.Combine(root,"adaptive.bin");
  Directory.CreateDirectory(old+".mehrparts");File.WriteAllText(Path.Combine(old+".mehrparts","state.json"),new JavaScriptSerializer().Serialize(new DownloadState{Url=url,Size=256L*1048576,Validator="\"adaptive-v1\"",Connections=8}));
  var timer=Stopwatch.StartNew();var fixedEngine=new DownloadEngine();fixedEngine.Run(url,old,8).Wait();double fixedSeconds=timer.Elapsed.TotalSeconds;Check(fixedEngine.Error==null&&File.Exists(old),"legacy fixed-layout downloads remain compatible: "+fixedEngine.Error);
  timer.Restart();var adaptive=new DownloadEngine();adaptive.Run(url,fresh,8).Wait();double adaptiveSeconds=timer.Elapsed.TotalSeconds;Check(adaptive.Error==null&&File.Exists(fresh)&&adaptive.State.PartCount>8,"fast workers take new chunks when their part completes");Check(Hash(old)==Hash(fresh),"adaptive output exactly matches the legacy output");Check(adaptiveSeconds<fixedSeconds*.8,"adaptive scheduler reduces slow-tail completion time");Console.WriteLine("BENCHMARK: fixed="+fixedSeconds.ToString("0.00")+"s adaptive="+adaptiveSeconds.ToString("0.00")+"s");
  string resumed=Path.Combine(root,"resumed.bin");var paused=new DownloadEngine();var work=paused.Run(url,resumed,8);while(paused.Received<16*1048576&&paused.Running)Thread.Sleep(20);paused.Pause();work.Wait();long saved=paused.Received;Check(saved>0&&!File.Exists(resumed),"adaptive pause preserves partial chunks without publishing incomplete output");
  var next=new DownloadEngine();next.Run(url,resumed,16).Wait();Check(next.Error==null&&Hash(resumed)==Hash(fresh),"resume preserves chunks even when connection count changes");Check(next.SessionReceived==next.State.Size-saved,"speed counter excludes bytes from the previous session");
  Console.WriteLine("ADAPTIVE TESTS PASSED");
 }
}
