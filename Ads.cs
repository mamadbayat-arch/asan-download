using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Reflection;

namespace MehrDownload {
 class Advertisement {
  public bool active=true;
  public string title="همتوار؛ همراه روزهای منظم‌تر",description="مدیریت کارها، دخل‌وخرج و هدف‌ها در یک برنامهٔ فارسی.",ctaText="آشنایی با همتوار",link="https://hamtavar.ir",imageUrl="/assets/hamtavar.png";
  public int revision=1;
 }
 class AdPlacement : GlassPanel {
  public const string Origin="https://downloader.hamtavar.ir";
  public const string LegacyOrigin="https://asan-download.woolly-frog-8626.chatgpt.site";
  readonly Label heading=new Label(),description=new Label();
  readonly PictureBox picture=new PictureBox();readonly GlassButton action=new GlassButton();
  readonly Timer refresh=new Timer();bool loading,disposed;Advertisement ad=new Advertisement();
  readonly string cache;
  public AdPlacement(bool online) {
   cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","ad.json");
   Padding=new Padding(12);Radius=18;Dock=DockStyle.Fill;
   var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};row.RowStyles.Add(new RowStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));Controls.Add(row);
   picture.Dock=DockStyle.Fill;picture.SizeMode=PictureBoxSizeMode.Zoom;picture.BackColor=Color.White;picture.Image=Embedded("Hamtavar.png");row.Controls.Add(picture,0,0);
   var texts=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};texts.RowStyles.Add(new RowStyle(SizeType.Percent,50));texts.RowStyles.Add(new RowStyle(SizeType.Percent,50));
   heading.Dock=description.Dock=DockStyle.Fill;heading.BackColor=description.BackColor=Color.Transparent;heading.Font=Brand.Font(10,FontStyle.Bold);description.Font=Brand.Font(9);heading.AutoEllipsis=description.AutoEllipsis=true;heading.Margin=description.Margin=Padding.Empty;
   texts.Controls.Add(heading,0,0);texts.Controls.Add(description,0,1);row.Controls.Add(texts,1,0);
   action.Dock=DockStyle.Fill;action.Font=Brand.Font(10);row.Controls.Add(action,2,0);action.Click+=(s,e)=>{Uri target;if(Uri.TryCreate(ad.link,UriKind.Absolute,out target)&&(target.Scheme=="https"||target.Scheme=="http"))try{System.Diagnostics.Process.Start(target.AbsoluteUri);}catch(Exception ex){MessageBox.Show(ex.Message,"آسان دانلود");}};
   if(online && File.Exists(cache))try{ad=new JavaScriptSerializer().Deserialize<Advertisement>(File.ReadAllText(cache));}catch{}
   Apply();refresh.Interval=5*60*1000;refresh.Tick+=async(s,e)=>await UpdateAd();
   if(online){refresh.Start();HandleCreated+=async(s,e)=>await UpdateAd();}
  }
  public static Image Embedded(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var i=Image.FromStream(s))return new Bitmap(i);}
  void Apply(){heading.Text="تبلیغات  •  "+ad.title;description.Text=ad.description;action.Text=ad.ctaText;foreach(Control c in Controls)c.Visible=ad.active;}
  static byte[] Get(string url,int maximum) {
   try{return GetRaw(url,maximum);}catch{if(url.StartsWith(Origin+"/",StringComparison.Ordinal))return GetRaw(LegacyOrigin+url.Substring(Origin.Length),maximum);throw;}
  }
  static byte[] GetRaw(string url,int maximum) {
   var request=DownloadEngine.Request(url);request.Timeout=request.ReadWriteTimeout=8000;
   using(var response=(HttpWebResponse)request.GetResponse())using(var input=response.GetResponseStream())using(var output=new MemoryStream()) {
    if(response.ContentLength>maximum)throw new IOException("Ad too large");byte[] buffer=new byte[16384];int read;
    while((read=input.Read(buffer,0,buffer.Length))>0){if(output.Length+read>maximum)throw new IOException("Ad too large");output.Write(buffer,0,read);}return output.ToArray();
   }
  }
  async Task UpdateAd(){
   if(loading||disposed)return;loading=true;
   try {
    var result=await Task.Run(()=>{
     var next=new JavaScriptSerializer().Deserialize<Advertisement>(System.Text.Encoding.UTF8.GetString(Get(Origin+"/api/public/ad",32768)));
     if(next==null||next.title==null||next.title.Length>100||next.description==null||next.description.Length>240||next.ctaText==null||next.ctaText.Length>40)throw new IOException("Invalid advertisement");
     Uri link;if(!Uri.TryCreate(next.link,UriKind.Absolute,out link)||(link.Scheme!="https"&&link.Scheme!="http"))throw new IOException("Invalid link");
     if(next.imageUrl!="/assets/hamtavar.png"&&!System.Text.RegularExpressions.Regex.IsMatch(next.imageUrl??"",@"^/media/[a-f0-9]{32}\.(png|jpg|webp)$"))throw new IOException("Invalid image path");
     return next;
    });
    Image image=null;
    if(result.active&&result.imageUrl!="/assets/hamtavar.png")try{byte[] bytes=await Task.Run(()=>Get(Origin+result.imageUrl,4*1024*1024));using(var s=new MemoryStream(bytes))using(var raw=Image.FromStream(s)){if(raw.Width>6000||raw.Height>6000)throw new IOException("Invalid image size");image=new Bitmap(raw);}}catch{}
    if(disposed){if(image!=null)image.Dispose();return;}
    ad=result;Apply();var old=picture.Image;picture.Image=image??Embedded("Hamtavar.png");if(old!=null)old.Dispose();
    Directory.CreateDirectory(Path.GetDirectoryName(cache));File.WriteAllText(cache,new JavaScriptSerializer().Serialize(ad));
   }catch{}finally{loading=false;}
  }
  protected override void Dispose(bool disposing){disposed=true;if(disposing){refresh.Stop();refresh.Dispose();if(picture.Image!=null)picture.Image.Dispose();}base.Dispose(disposing);}
 }
}
