using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Net;
class NativeHost {
 static byte[] Read(Stream stream,int length){byte[] bytes=new byte[length];for(int offset=0;offset<length;){int read=stream.Read(bytes,offset,length-offset);if(read==0)throw new EndOfStreamException();offset+=read;}return bytes;}
 static string Deliver(string command){try{using(var pipe=new NamedPipeClientStream(".","AsanDownload.Desktop.v1",PipeDirection.InOut)){pipe.Connect(1500);using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true)){writer.AutoFlush=true;writer.WriteLine(command);}using(var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true)){var read=reader.ReadLineAsync();return read.Wait(command=="ping"?3000:120000)?read.Result:"timeout";}}}catch{return null;}}
 static string OpenAndDeliver(string command){string result=Deliver(command);if(result!=null)return result;string exe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"MehrDownload.exe");if(!File.Exists(exe))return "missing";Process.Start(new ProcessStartInfo(exe,"--browser-start"){UseShellExecute=false});for(int n=0;n<12;n++){System.Threading.Thread.Sleep(500);result=Deliver(command);if(result!=null)return result;}return "unavailable";}
 static bool PublicFile(string url){
  ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
  foreach(bool range in new[]{false,true})try{var request=(HttpWebRequest)WebRequest.Create(url);request.Method=range?"GET":"HEAD";request.Timeout=5000;request.ReadWriteTimeout=5000;request.UserAgent="MehrDownload/1.7";if(range)request.AddRange(0,0);
   using(var response=(HttpWebResponse)request.GetResponse()){return (response.StatusCode==HttpStatusCode.OK||response.StatusCode==HttpStatusCode.PartialContent)&&!response.ContentType.StartsWith("text/html",StringComparison.OrdinalIgnoreCase)&&!response.ContentType.StartsWith("application/xhtml",StringComparison.OrdinalIgnoreCase);}
  }catch{}return false;
 }
 static void Main(){string result="invalid";try{using(var input=Console.OpenStandardInput()){int length=BitConverter.ToInt32(Read(input,4),0);if(length<1||length>16384)throw new Exception();var data=new JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string,string>>(Encoding.UTF8.GetString(Read(input,length)));
   string action;if(data.TryGetValue("action",out action)&&(action=="ping"||action=="open")){result=OpenAndDeliver(action=="open"?"activate":"ping");}
   else{string url;Uri uri;if(!data.TryGetValue("url",out url)||url.Length>8192||!Uri.TryCreate(url,UriKind.Absolute,out uri)||!(uri.Scheme=="http"||uri.Scheme=="https")||!String.IsNullOrEmpty(uri.UserInfo))throw new Exception();result=PublicFile(url)?OpenAndDeliver(url):"browser_required";}
  }}catch{}try{byte[] response=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(new{accepted=result=="accepted",cancelled=result=="cancelled",reason=result}));using(var output=Console.OpenStandardOutput()){output.Write(BitConverter.GetBytes(response.Length),0,4);output.Write(response,0,response.Length);output.Flush();}}catch{}}
}
