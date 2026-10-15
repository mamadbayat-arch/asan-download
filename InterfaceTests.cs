using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using MehrDownload;
class InterfaceTests {
 static void Check(bool value,string description){if(!value)throw new Exception(description);Console.WriteLine("PASS: "+description);}
 static object Field(Window window,string name){return typeof(Window).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);}
 [STAThread] static void Main(){
  System.Net.ServicePointManager.DefaultConnectionLimit=32;Application.EnableVisualStyles();
  string root=Path.Combine(Environment.CurrentDirectory,"interface-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  string settings=Path.Combine(root,"session.json"), folder=Path.Combine(root,"downloads");Directory.CreateDirectory(folder);
  var saved=new Saved{Folder=folder,Url="http://localhost/old.zip",Target=Path.Combine(folder,"old.zip")};saved.Save(settings);
  Check(Saved.Load(settings).Folder==folder,"default folder survives restart");
  var calendarSample=new CompletedFile{Finished=new DateTime(2026,3,21,9,5,0),DurationSeconds=3661,AverageBytesPerSecond=1000000};
  Check(calendarSample.PersianDate()=="۱۴۰۵/۰۱/۰۱ ۰۹:۰۵","Persian calendar date handles the new-year boundary");Check(calendarSample.Duration()=="۰۱:۰۱:۰۱"&&calendarSample.Average(false)=="۱.۰۰ MB/s"&&calendarSample.Average(true)=="۸.۰۰ Mb/s","duration and average preserve hours and byte-to-bit conversion");Check(new CompletedFile().Duration()=="—"&&new CompletedFile().Average(false)=="—","legacy history never invents download metrics");
  File.WriteAllText(Path.Combine(folder,"new.zip"),"existing");
  Check(saved.TargetFor("http://localhost/new.zip")==Path.Combine(folder,"new (1).zip"),"duplicate names preserve existing downloads");
  Directory.CreateDirectory(saved.Target+".mehrparts");
  Check(saved.TargetFor(saved.Url)==saved.Target,"paused target reused for the same link");
  using(var window=new Window(true,settings)){
   window.Show();Application.DoEvents();
   Check(window.Text=="آسان دانلود" && window.Icon!=null,"new app name and icon applied");
   Check(window.Font.Name.Contains("Kalameh"),"Kalameh font loaded from embedded resources");
   Check(!window.RightToLeftLayout,"native window mirroring disabled to prevent clipped sidebar and broken painting");
   Check(window.FormBorderStyle==FormBorderStyle.None,"custom modern title bar replaces native window buttons");
   var bar=(TitleBar)typeof(GlassForm).GetField("titleBar",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
   var close=(WindowActionButton)typeof(TitleBar).GetField("close",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bar);
   var maximize=(WindowActionButton)typeof(TitleBar).GetField("maximize",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bar);
   var minimize=(WindowActionButton)typeof(TitleBar).GetField("minimize",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bar);
   Check(close.Left>maximize.Left && maximize.Left>minimize.Left && close.Right<=bar.Width,"close, maximize and minimize arranged at right edge");
   var normalBounds=window.Bounds;maximize.PerformClick();Application.DoEvents();Check(window.WindowState==FormWindowState.Maximized && maximize.Restored,"maximize button switches to restore icon");maximize.PerformClick();Application.DoEvents();Check(window.WindowState==FormWindowState.Normal && window.Bounds==normalBounds,"restore returns original bounds");
   minimize.PerformClick();Application.DoEvents();Check(window.WindowState==FormWindowState.Minimized,"minimize button works");window.WindowState=FormWindowState.Normal;Application.DoEvents();
   Check(!((Control)Field(window,"locationRow")).Visible,"folder selection row disappears after restoring default folder");
   var summary=(GlassButton)Field(window,"linkSummary");
   Check(!typeof(System.Windows.Forms.Button).IsAssignableFrom(summary.GetType()),"buttons use custom controls without native Windows button frames");
   var link=(TextBox)Field(window,"link");var target=(TextBox)Field(window,"destination");
   link.Text="https://example.com/file.zip?token=keep-the-entire-link";
   Check(!summary.Text.Contains("token=") && link.Text.EndsWith("token=keep-the-entire-link"),"compact link hides query without changing actual download URL");
   link.Text="https://example.com/new.zip";
   Check(target.Text==Path.Combine(folder,"new (1).zip"),"pasted new URL immediately updates filename");
   link.Text="http://127.0.0.1:18743/range";
   Check(target.Text==Path.Combine(folder,"range"),"same folder reused without a dialog");
   using(var prompt=new DownloadPrompt("https://example.com/sample.zip","sample.zip",false,false)){prompt.Show(window);Application.DoEvents();Check(prompt.Visible&&prompt.CancelButton!=null,"download confirmation shows before starting and allows cancellation");prompt.DialogResult=DialogResult.Cancel;prompt.Close();}
   var work=(Task)typeof(Window).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{true});
   while(!work.IsCompleted){Application.DoEvents();Thread.Sleep(10);}work.GetAwaiter().GetResult();
   var list=(ListView)Field(window,"history");
   Check(list.Items.Count==1 && File.Exists(Path.Combine(folder,"range")),"successful download appears in completed list");
   Check(!((Label)Field(window,"emptyHint")).Visible,"empty state disappears after successful download");
   Check(list.ContextMenuStrip.Items.Count==2,"file open and reveal-in-folder commands exist");
   Check(Saved.Load(settings).History.Count==1,"completed list saved on disk");
   var recorded=Saved.Load(settings).History[0];Check(recorded.DurationSeconds>0&&recorded.AverageBytesPerSecond>0,"actual download duration and average speed persist on disk");Check(list.Columns.Count==5&&list.Items[0].SubItems[2].Text.StartsWith("۱۴")&&list.Items[0].SubItems[3].Text!="—"&&list.Items[0].SubItems[4].Text.EndsWith("MB/s"),"history displays Persian date, duration and average speed");
   using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(0,0,window.Width,window.Height));image.Save(Path.Combine(Environment.CurrentDirectory,"preview.png"));}
   typeof(Window).GetField("dark",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,true);
   typeof(Window).GetMethod("PaintTheme",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{window});
   using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(0,0,window.Width,window.Height));image.Save(Path.Combine(Environment.CurrentDirectory,"preview-dark.png"));}
   close.PerformClick();Application.DoEvents();Check(window.IsDisposed,"close button closes the window");
  }
  using(var restored=new Window(true,settings))Check(((ListView)Field(restored,"history")).Items.Count==1,"completed list restored after restart");
  string legacy=Path.Combine(root,"legacy.json");File.WriteAllText(legacy,"{\"Url\":\"https://example.com/file.zip\",\"Target\":"+new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Path.Combine(folder,"file.zip"))+",\"Connections\":8}");
  Check(Saved.Load(legacy).Folder==folder,"previous version folder migrated");
  string durable=Path.Combine(root,"durable.json");var first=new Saved{Folder=folder};first.Save(durable);var stale=Saved.Load(durable);
  first.History.Add(new CompletedFile{Path=Path.Combine(folder,"removed.zip"),Size=123,Finished=DateTime.Now,DurationSeconds=10,AverageBytesPerSecond=12.3});first.Save(durable);
  stale.History.Add(new CompletedFile{Path=Path.Combine(folder,"new-download.zip"),Size=456,Finished=DateTime.Now.AddSeconds(1)});stale.Save(durable);
  Check(Saved.Load(durable).History.Count==2,"stale settings writer cannot erase earlier completed downloads");
  first.Save(durable);first.Save(durable);Check(Saved.Load(durable).History.Count==2,"repeated saves do not duplicate history and retain moved or deleted files");
  first.Complete(Path.Combine(folder,"new.zip"));first.Save(durable);first.Complete(Path.Combine(folder,"new.zip"));first.Save(durable);Check(Saved.Load(durable).History.Count==4,"downloading to the same path again preserves both completed events");
  File.WriteAllText(durable,"broken JSON");var recovered=Saved.Load(durable);Check(recovered.History.Count==4&&recovered.Folder==folder,"damaged settings recover from backup and independent history records");
  File.WriteAllText(durable+".bak","broken backup");Check(Saved.Load(durable).History.Count==4,"history survives loss of both settings copies");recovered.Save(durable);
  Check(Directory.GetFiles(root,"durable.json.corrupt-*").Length==1,"malformed settings preserved as recovery evidence");
  var restoredRecords=Saved.Load(durable).History;Check(restoredRecords.Exists(f=>f.DurationSeconds==10&&f.AverageBytesPerSecond==12.3),"history recovery retains recorded duration and average speed");
  for(int n=0;n<3;n++)using(var reopened=new Window(true,durable))Check(((ListView)Field(reopened,"history")).Items.Count==4,"history survives closing and reopening the app repeatedly");
  string large=Path.Combine(root,"large.json");var lots=new Saved();for(int n=0;n<2200;n++)lots.History.Add(new CompletedFile{Id=n.ToString(),Path=new string('a',1000)+n.ToString(),Finished=DateTime.Now});
  File.WriteAllText(large,new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=Int32.MaxValue}.Serialize(lots));Check(Saved.Load(large).History.Count==2200,"large history exceeds the old two-megabyte serializer limit without disappearing");
  Console.WriteLine("INTERFACE TESTS PASSED");
 }
}
