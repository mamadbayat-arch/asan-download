using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using MehrDownload;
class UpdateTests {
 static void Check(bool condition,string text){if(!condition)throw new Exception(text);Console.WriteLine("PASS: "+text);}
 static void Reject(Action action,string text){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,text);}
 static void Sign(ReleaseManifest r,RSACryptoServiceProvider rsa){r.signature=Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(r.Payload),CryptoConfig.MapNameToOID("SHA256")));}
 [STAThread]static void Main(){
  string root=Path.Combine(Environment.CurrentDirectory,"update-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  using(var rsa=new RSACryptoServiceProvider(2048)){
   rsa.PersistKeyInCsp=false;var client=new UpdateClient(rsa.ToXmlString(false));byte[] executable=File.ReadAllBytes("MehrDownload.exe");
   var release=new ReleaseManifest{available=true,version="99.0.0",updateUrl="/downloads/AsanDownload-Update.exe",sha256=UpdateClient.Hash(executable),exeSize=executable.Length};Sign(release,rsa);
   Check(client.Verify(release),"publisher-signed manifest accepted");
   release.version="9.9.9";Check(!client.Verify(release),"changed version invalidates signature");release.version="99.0.0";
   release.updateUrl="https://evil.example/payload.exe";Check(!client.Verify(release),"foreign update URLs rejected");release.updateUrl="/downloads/AsanDownload-Update.exe";
   Check(UpdateClient.IsNewer("1.10.0","1.9.0")&&!UpdateClient.IsNewer("1.5.0","1.5.0")&&!UpdateClient.IsNewer("1.4.0","1.5.0"),"semantic version comparison prevents reinstall and downgrade");
   using(var other=new RSACryptoServiceProvider(2048)){other.PersistKeyInCsp=false;Check(!new UpdateClient(other.ToXmlString(false)).Verify(release),"untrusted signing key rejected");}
   var listener=new TcpListener(IPAddress.Loopback,18744);listener.Start();int mode=0;bool serving=true;
   var server=Task.Run(()=>{while(serving){TcpClient connection;try{connection=listener.AcceptTcpClient();}catch{break;}using(connection)try{using(var stream=connection.GetStream()){
    string head="";while(!head.EndsWith("\r\n\r\n")){int value=stream.ReadByte();if(value<0)break;head+=(char)value;if(head.Length>16384)break;}
    bool manifest=head.StartsWith("GET /api/public/release ");byte[] bytes=manifest?Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(release)):(byte[])executable.Clone();
    if(mode==1&&!manifest)bytes[bytes.Length-1]^=1;
    string headers=mode==2?"HTTP/1.1 302 Found\r\nLocation: http://127.0.0.1:18744/untrusted\r\nContent-Length: 0\r\nConnection: close\r\n\r\n":"HTTP/1.1 200 OK\r\nContent-Length: "+bytes.Length+"\r\nConnection: close\r\n\r\n";
    byte[] prefix=Encoding.ASCII.GetBytes(headers);stream.Write(prefix,0,prefix.Length);if(mode!=2)stream.Write(bytes,0,bytes.Length);
   }}catch{}}});
   try{
    var origin=new Uri("http://127.0.0.1:18744");Check(client.Check(origin,CancellationToken.None)!=null,"startup sees a signed newer release");
    long progress=0;string downloaded=client.Download(origin,release,root,CancellationToken.None,(done,total)=>progress=done);
    Check(File.Exists(downloaded)&&client.CheckFile(downloaded,release)&&progress==executable.Length,"download progress, length and SHA-256 verified before staging");
    mode=1;Reject(()=>client.Download(origin,release,root,CancellationToken.None,null),"tampered update bytes never staged");mode=2;Reject(()=>client.Check(origin,CancellationToken.None),"redirected manifests rejected");mode=0;
    using(var cancel=new CancellationTokenSource()){cancel.Cancel();Reject(()=>client.Download(origin,release,root,cancel.Token,null),"cancelled update does not install");}
   }finally{serving=false;listener.Stop();server.Wait(2000);}
   string target=Path.Combine(root,"installed.exe");File.WriteAllText(target,"previous version");string settings=Path.Combine(root,"session.json");File.WriteAllText(settings,"existing settings and history");
   UpdateClient.ReplaceFile(Path.Combine(root,Directory.GetFiles(root,"*.exe")[0]),target);
   Check(UpdateClient.Hash(File.ReadAllBytes(target))==release.sha256&&File.ReadAllText(target+".previous")=="previous version","atomic replacement preserves the previous executable");
   UpdateClient.Rollback(target);Check(File.ReadAllText(target)=="previous version"&&File.ReadAllText(settings)=="existing settings and history","rollback restores app and leaves settings unchanged");
   using(var held=File.Open(target,FileMode.Open,FileAccess.Read,FileShare.None))Reject(()=>UpdateClient.ReplaceFile(Directory.GetFiles(root,"*.exe")[0],target),"running or locked app blocks replacement without corruption");
   Check(File.ReadAllText(target)=="previous version","failed replacement preserves installed bytes");
  }
  var published=new JavaScriptSerializer().Deserialize<ReleaseManifest>(File.ReadAllText("server/public/release.json"));Check(new UpdateClient().CheckFile("server/public/downloads/AsanDownload-Update.exe",published),"real release uses the pinned publisher key");
  Console.WriteLine("UPDATE TESTS PASSED");
 }
}
