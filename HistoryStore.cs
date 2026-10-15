using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace MehrDownload {
 // Completed downloads have independent durable records. Settings and stale instances
 // cannot replace or prune these records, even if a downloaded file is moved/deleted.
 static class HistoryStore {
  static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=Int32.MaxValue};}
  static string Hash(string value){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
  static string Key(CompletedFile file){
   if(String.IsNullOrEmpty(file.Id))file.Id=Hash((file.Path??"").ToUpperInvariant()+"\n"+(file.Finished.ToUniversalTime().Ticks/TimeSpan.TicksPerMillisecond).ToString(System.Globalization.CultureInfo.InvariantCulture));
   return Hash(file.Id).Substring(0,32);
  }
  internal static Saved ReadSession(string path){try{return File.Exists(path)?Json().Deserialize<Saved>(File.ReadAllText(path)):null;}catch(ArgumentException){return null;}catch(InvalidOperationException){return null;}}
  static CompletedFile ReadRecord(string path){try{return File.Exists(path)?Json().Deserialize<CompletedFile>(File.ReadAllText(path)):null;}catch(ArgumentException){return null;}catch(InvalidOperationException){return null;}}
  static void Add(Dictionary<string,CompletedFile> records, IEnumerable<CompletedFile> files){
   if(files==null)return;foreach(var file in files){if(file==null||String.IsNullOrEmpty(file.Path))continue;string key=Key(file);CompletedFile existing;
    if(!records.TryGetValue(key,out existing))records.Add(key,file);
    else{if(!existing.DurationSeconds.HasValue)existing.DurationSeconds=file.DurationSeconds;if(!existing.AverageBytesPerSecond.HasValue)existing.AverageBytesPerSecond=file.AverageBytesPerSecond;}
   }
  }
  internal static void Merge(string path,List<CompletedFile> history){
   var records=new Dictionary<string,CompletedFile>(StringComparer.Ordinal);Add(records,history);
   foreach(string candidate in new[]{path,path+".bak"}){var saved=ReadSession(candidate);if(saved!=null)Add(records,saved.History);}
   string directory=path+".history";
   if(Directory.Exists(directory))foreach(string entry in Directory.EnumerateFiles(directory,"*.json")){var file=ReadRecord(entry)??ReadRecord(entry+".bak");if(file!=null)Add(records,new[]{file});}
   history.Clear();history.AddRange(records.Values);history.Sort((a,b)=>b.Finished.CompareTo(a.Finished));
  }
  static void AtomicWrite(string path,string content){
   string temp=path+".tmp-"+Guid.NewGuid().ToString("N").Substring(0,8);
   try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
     var bytes=Encoding.UTF8.GetBytes(content);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
    }
    if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
   }finally{if(File.Exists(temp))File.Delete(temp);}
  }
  internal static void Save(string path,Saved saved){
   path=System.IO.Path.GetFullPath(path);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
   using(var gate=new Mutex(false,"Local\\AsanDownload.History."+Hash(path.ToUpperInvariant()))){
    bool owned=false;try{try{owned=gate.WaitOne(TimeSpan.FromSeconds(15));}catch(AbandonedMutexException){owned=true;}if(!owned)throw new IOException("ذخیرهٔ سابقه مشغول است؛ دوباره تلاش کنید.");
     if(saved.History==null)saved.History=new List<CompletedFile>();Merge(path,saved.History);
     string directory=path+".history";Directory.CreateDirectory(directory);
     foreach(var file in saved.History){string record=System.IO.Path.Combine(directory,Key(file)+".json");string content=Json().Serialize(file);if(!File.Exists(record)||File.ReadAllText(record)!=content)AtomicWrite(record,content);}
     // Preserve malformed legacy settings for recovery instead of overwriting evidence.
     if(File.Exists(path)&&ReadSession(path)==null)File.Copy(path,path+".corrupt-"+Guid.NewGuid().ToString("N"),false);
     AtomicWrite(path,Json().Serialize(saved));
    }finally{if(owned)gate.ReleaseMutex();}
   }
  }
 }
}
