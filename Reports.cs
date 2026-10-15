using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Windows.Forms;
namespace MehrDownload {
 class ProblemReport {public string id=Guid.NewGuid().ToString("N"),category,title,description,contact,version=ReleaseInfo.Version;}
 static class ProblemReports {
  static readonly object gate=new object();static bool sending;
  static string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","reports.json");
  static List<ProblemReport> Read(){return File.Exists(path)?new JavaScriptSerializer().Deserialize<List<ProblemReport>>(File.ReadAllText(path)):new List<ProblemReport>();}
  static void Write(List<ProblemReport> items){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",new JavaScriptSerializer().Serialize(items));if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);}
  public static void Enqueue(ProblemReport report){lock(gate){var list=Read();if(list.Count>=100)throw new Exception("صف گزارش‌ها پر است؛ پس از اتصال به اینترنت دوباره تلاش کنید.");list.Add(report);Write(list);}}
  public static async Task<bool> Flush(string id=null){
   lock(gate){if(sending)return false;sending=true;}
   try{return await Task.Run(()=>{
    List<ProblemReport> pending;lock(gate)pending=Read();
    foreach(var report in pending){bool delivered=false;foreach(string origin in new[]{AdPlacement.Origin,AdPlacement.LegacyOrigin})try{
     var request=(HttpWebRequest)WebRequest.Create(origin+"/api/public/reports");request.Method="POST";request.ContentType="application/json";request.Timeout=5000;request.ReadWriteTimeout=5000;request.AllowAutoRedirect=false;
     byte[] bytes=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(report));request.ContentLength=bytes.Length;using(var stream=request.GetRequestStream())stream.Write(bytes,0,bytes.Length);
     using(var response=(HttpWebResponse)request.GetResponse()){delivered=response.StatusCode==HttpStatusCode.Created||response.StatusCode==HttpStatusCode.Accepted;}
     if(delivered)break;
    }catch{}
    if(!delivered)break;lock(gate){var list=Read();list.RemoveAll(r=>r.id==report.id);Write(list);}
    }lock(gate)return id!=null&&!Read().Exists(r=>r.id==id);
   });}catch{return false;}finally{lock(gate)sending=false;}
  }
 }
 class ReportDialog:GlassForm {
  public ReportDialog(){
   Text="گزارش مشکل";ClientSize=new Size(570,620);StartPosition=FormStartPosition.CenterParent;Font=Brand.Font(12);RightToLeft=RightToLeft.Yes;
   var body=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=10};foreach(int h in new[]{38,28,38,26,38,26,110,26,38,68})body.RowStyles.Add(new RowStyle(SizeType.Absolute,h));Controls.Add(body);
   body.Controls.Add(new Label{Text="کمک کنید آسان دانلود بهتر شود",Font=Brand.Font(17,FontStyle.Bold),Dock=DockStyle.Fill},0,0);
   body.Controls.Add(new Label{Text="نوع مشکل",Dock=DockStyle.Fill},0,1);var category=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList};category.Items.AddRange(new object[]{"ظاهر برنامه","دانلود","به‌روزرسانی","سایر"});category.SelectedIndex=0;body.Controls.Add(category,0,2);
   body.Controls.Add(new Label{Text="عنوان کوتاه",Dock=DockStyle.Fill},0,3);var title=new TextBox{Dock=DockStyle.Fill,MaxLength=120};body.Controls.Add(title,0,4);
   body.Controls.Add(new Label{Text="چه اتفاقی افتاد؟ انتظار داشتید چه شود؟",Dock=DockStyle.Fill},0,5);var description=new TextBox{Dock=DockStyle.Fill,Multiline=true,MaxLength=4000,ScrollBars=ScrollBars.Vertical};body.Controls.Add(description,0,6);
   body.Controls.Add(new Label{Text="ایمیل یا شماره تماس (اختیاری)",Dock=DockStyle.Fill},0,7);var contact=new TextBox{Dock=DockStyle.Fill,MaxLength=160,RightToLeft=RightToLeft.No};body.Controls.Add(contact,0,8);
   var foot=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var send=new GlassButton{Text="ارسال گزارش",Primary=true,Size=new Size(150,44)};var note=new Label{Text="نسخهٔ "+ReleaseInfo.Version+" • فقط متن این فرم ارسال می‌شود.",AutoSize=true,Font=Brand.Font(9),Margin=new Padding(8,15,0,0)};foot.Controls.Add(send);foot.Controls.Add(note);body.Controls.Add(foot,0,9);
   send.Click+=async(s,e)=>{if(String.IsNullOrWhiteSpace(title.Text)||String.IsNullOrWhiteSpace(description.Text)){MessageBox.Show(this,"عنوان و توضیح مشکل را وارد کنید.",Text);return;}
    var report=new ProblemReport{category=category.Text,title=title.Text.Trim(),description=description.Text.Trim(),contact=contact.Text.Trim()};
    try{ProblemReports.Enqueue(report);send.Enabled=false;note.Text="در حال ارسال…";bool sent=await ProblemReports.Flush(report.id);MessageBox.Show(this,sent?"گزارش شما ارسال شد. ممنون از همراهی شما.":"گزارش ذخیره شد؛ با اتصال اینترنت خودکار ارسال می‌شود.",Text);Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);send.Enabled=true;}
   };
  }
 }
}
