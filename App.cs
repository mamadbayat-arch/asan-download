using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Globalization;

[assembly: System.Reflection.AssemblyVersion("1.8.2.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.8.2.0")]
namespace MehrDownload {
 class CompletedFile {
  public string Id,Path;public long Size;public DateTime Finished;
  public double? DurationSeconds,AverageBytesPerSecond;
  public static string PersianDigits(string value){for(int n=0;n<10;n++)value=value.Replace((char)('0'+n),(char)('۰'+n));return value;}
  public string PersianDate(){if(Finished==DateTime.MinValue)return "—";var date=Finished.Kind==DateTimeKind.Utc?Finished.ToLocalTime():Finished;var calendar=new PersianCalendar();return PersianDigits(String.Format(CultureInfo.InvariantCulture,"{0:0000}/{1:00}/{2:00} {3:00}:{4:00}",calendar.GetYear(date),calendar.GetMonth(date),calendar.GetDayOfMonth(date),date.Hour,date.Minute));}
  public string Duration(){if(!DurationSeconds.HasValue)return "—";long seconds=(long)Math.Ceiling(Math.Max(0,DurationSeconds.Value));return PersianDigits(String.Format(CultureInfo.InvariantCulture,"{0:00}:{1:00}:{2:00}",seconds/3600,(seconds/60)%60,seconds%60));}
  public string Average(bool bits){return AverageBytesPerSecond.HasValue?PersianDigits((AverageBytesPerSecond.Value*(bits?8:1)/1000000d).ToString("0.00",CultureInfo.InvariantCulture))+(bits?" Mb/s":" MB/s"):"—";}
 }
 class Saved {
  public string Url,Target,Folder;
  public int Connections=8;
  public bool SpeedInBits;
  public List<CompletedFile> History=new List<CompletedFile>();
  public List<string> Queue=new List<string>();
  public static Saved Load(string path) {
   Saved saved=HistoryStore.ReadSession(path) ?? HistoryStore.ReadSession(path+".bak") ?? new Saved();
   if(saved.History==null)saved.History=new List<CompletedFile>();
   if(saved.Queue==null)saved.Queue=new List<string>();
   if(String.IsNullOrEmpty(saved.Folder) && !String.IsNullOrEmpty(saved.Target)) saved.Folder=System.IO.Path.GetDirectoryName(saved.Target);
   HistoryStore.Merge(path,saved.History);return saved;
  }
  public void Save(string path) {
   HistoryStore.Save(path,this);
  }
  public string TargetFor(string url) {
   string name=DownloadEngine.FileName(url);
   if(String.IsNullOrEmpty(Folder)) return name;
   if(Url==url && !String.IsNullOrEmpty(Target) && System.IO.Path.GetDirectoryName(Target)==Folder && !File.Exists(Target) && Directory.Exists(Target+".mehrparts")) return Target;
   string candidate=System.IO.Path.Combine(Folder,name);
   for(int n=1;File.Exists(candidate) || Directory.Exists(candidate+".mehrparts");n++) candidate=System.IO.Path.Combine(Folder,System.IO.Path.GetFileNameWithoutExtension(name)+" ("+n+")"+System.IO.Path.GetExtension(name));
   return candidate;
  }
  public void Complete(string path,DownloadState metrics=null) {
   var file=new CompletedFile {Id=Guid.NewGuid().ToString("N"),Path=path,Size=new FileInfo(path).Length,Finished=DateTime.Now};if(metrics!=null&&metrics.HasTiming&&metrics.ActiveSeconds>0){file.DurationSeconds=metrics.ActiveSeconds;file.AverageBytesPerSecond=file.Size/metrics.ActiveSeconds;}History.Insert(0,file);
  }
 }
 class Window : GlassForm {
  TextBox link=new TextBox(), destination=new TextBox();
  GlassButton linkSummary;
  TableLayoutPanel downloadLayout;
  TableLayoutPanel contentLayout;
  Control locationRow;
  Label fileSummary;
  ToolTip hints=new ToolTip();
  NumericUpDown connections=new NumericUpDown();
  GlassButton start, pause, browse, folder, theme, paste;
  ListView history=new ListView();
  Saved saved=new Saved();
  Label status, speed, detail, count, total,emptyHint;
  GlassProgress progress=new GlassProgress();
  DownloadEngine engine=new DownloadEngine();
  Timer timer=new Timer();
  long previous; DateTime previousTime=DateTime.UtcNow;
  readonly DownloadSpeed downloadSpeed=new DownloadSpeed();
  double displayedRate;
  internal bool LastDownloadDeclined;
  string currentUrl,currentTarget; int currentConnections; bool closing, dark;
  string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","session.json");
  public Window(bool restore) : this(restore,null) {}
  internal Window(bool restore,string settingsPath) {
   if(settingsPath!=null)settings=settingsPath;
   Text="آسان دانلود";var screen=Screen.PrimaryScreen.WorkingArea;ClientSize=new Size(Math.Min(1100,screen.Width-24),Math.Min(900,screen.Height-24));MinimumSize=new Size(Math.Min(1000,screen.Width-24),Math.Min(870,screen.Height-24));
   StartPosition=FormStartPosition.CenterScreen;Font=Brand.Font(12);AutoScaleMode=AutoScaleMode.Dpi;
   RightToLeft=RightToLeft.Yes;RightToLeftLayout=false;
   using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Download.ico"))Icon=new Icon(stream);
   var shell=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(18),Margin=Padding.Empty};
   shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,220));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(shell);
   var sidebar=new GlassPanel{Dock=DockStyle.Fill,Padding=new Padding(16),Margin=new Padding(0,0,12,0)};
   shell.Controls.Add(sidebar,0,0);
   var navigation=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Margin=Padding.Empty};
   foreach(int h in new[]{86,48,48,48,48,48})navigation.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
   navigation.RowStyles.Add(new RowStyle(SizeType.Percent,100));navigation.RowStyles.Add(new RowStyle(SizeType.Absolute,48));navigation.RowStyles.Add(new RowStyle(SizeType.Absolute,64));sidebar.Controls.Add(navigation);
   var identity=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};identity.RowStyles.Add(new RowStyle(SizeType.Percent,100));identity.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,56));identity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));identity.Controls.Add(new LogoView{Dock=DockStyle.Fill,Margin=new Padding(0,12,0,12)},0,0);identity.Controls.Add(new Label{Text="آسان دانلود",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=Brand.Font(9,FontStyle.Bold),BackColor=Color.Transparent},1,0);navigation.Controls.Add(identity,0,0);
   var home=Button("دانلود جدید");home.Dock=DockStyle.Fill;navigation.Controls.Add(home,0,1);home.Click+=(s,e)=>{if(!engine.Running)EditLink();};
   var completed=Button("فایل‌های دریافت‌شده");completed.Dock=DockStyle.Fill;navigation.Controls.Add(completed,0,2);completed.Click+=(s,e)=>history.Focus();
   folder=Button("پوشهٔ دانلودها");folder.Dock=DockStyle.Fill;navigation.Controls.Add(folder,0,3);
   folder.Click+=(s,e)=>{try{if(!String.IsNullOrEmpty(saved.Folder)&&Directory.Exists(saved.Folder))Process.Start("explorer.exe","\""+saved.Folder+"\"");else ChooseFolder();}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};
   var settingsButton=Button("تنظیم پوشه");settingsButton.Dock=DockStyle.Fill;navigation.Controls.Add(settingsButton,0,4);settingsButton.Click+=(s,e)=>{if(!engine.Running)ChooseFolder();};
   var reportButton=Button("گزارش مشکل");reportButton.Dock=DockStyle.Fill;navigation.Controls.Add(reportButton,0,5);reportButton.Click+=(s,e)=>{using(var report=new ReportDialog())report.ShowDialog(this);};
   theme=Button("تیره / روشن");theme.Dock=DockStyle.Fill;navigation.Controls.Add(theme,0,7);theme.Click+=(s,e)=>{SuspendLayout();try{dark=!dark;PaintTheme(this);}finally{ResumeLayout(false);}Invalidate(true);};
   var integrationButton=Button("ویندوز و مرورگر");integrationButton.Dock=DockStyle.Fill;navigation.Controls.Add(integrationButton,0,8);integrationButton.Click+=(s,e)=>IntegrationSettings();
   var content=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(12,0,0,0),Margin=Padding.Empty};
   contentLayout=content;
   foreach(int h in new[]{70,104,342,16})content.RowStyles.Add(new RowStyle(SizeType.Absolute,h));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.Absolute,84));shell.Controls.Add(content,1,0);
   var header=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};header.RowStyles.Add(new RowStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));
   var titles=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};titles.RowStyles.Add(new RowStyle(SizeType.Absolute,40));titles.RowStyles.Add(new RowStyle(SizeType.Percent,100));var heading=Label("همهٔ دانلودها، یک‌جا");heading.Font=Brand.Font(19,FontStyle.Bold);heading.TextAlign=ContentAlignment.MiddleLeft;titles.Controls.Add(heading,0,0);var subheading=Label("لینک را وارد کنید؛ بقیه‌اش با آسان دانلود.");subheading.TextAlign=ContentAlignment.MiddleLeft;subheading.Font=Brand.Font(10);titles.Controls.Add(subheading,0,1);header.Controls.Add(titles,0,0);
   var badge=Label("آمادهٔ دریافت");badge.TextAlign=ContentAlignment.MiddleCenter;header.Controls.Add(badge,1,0);content.Controls.Add(header,0,0);
   var stats=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=new Padding(0,8,0,14)};stats.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   for(int i=0;i<3;i++)stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
   speed=Stat(stats,"سرعت دریافت","0 MB/s",0,Color.FromArgb(233,248,247));speed.RightToLeft=RightToLeft.No;
   ConfigureSpeedToggle(speed.Parent.Parent);
   count=Stat(stats,"فایل‌های کامل‌شده","۰",1,Color.FromArgb(243,235,253));
   total=Stat(stats,"حجم دریافت‌شده","0 MB",2,Color.FromArgb(255,241,234));total.RightToLeft=RightToLeft.No;content.Controls.Add(stats,0,1);
   var downloadPanel=new GlassPanel{Dock=DockStyle.Fill,Padding=new Padding(20),Margin=Padding.Empty};content.Controls.Add(downloadPanel,0,2);
   var layout=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Margin=Padding.Empty};
   downloadLayout=layout;
   foreach(int h in new[]{24,50,24,50,60,20,32,30})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));downloadPanel.Controls.Add(layout);
   layout.Controls.Add(Label("لینک مستقیم فایل"),0,0);
   var linkRow=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};linkRow.RowStyles.Add(new RowStyle(SizeType.Percent,100));linkRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));linkRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));
   link.RightToLeft=RightToLeft.No;link.Font=Brand.Font(12);link.AccessibleName="لینک دانلود";
   linkSummary=new GlassButton{Dock=DockStyle.Fill,Text="افزودن لینک دانلود…",RightToLeft=RightToLeft.No,Margin=new Padding(0,2,6,4),AccessibleName="نمایش و ویرایش لینک کامل"};linkRow.Controls.Add(linkSummary,0,0);linkSummary.Click+=(s,e)=>EditLink();
   paste=Button("چسباندن لینک");paste.Dock=DockStyle.Fill;linkRow.Controls.Add(paste,1,0);layout.Controls.Add(linkRow,0,1);
   paste.Click+=(s,e)=>{try{if(Clipboard.ContainsText()){link.Text=Clipboard.GetText().Trim();linkSummary.Focus();}else MessageBox.Show("کلیپ‌بورد متنی ندارد.",Text);}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};
   link.TextChanged+=(s,e)=>UpdateTarget();
   fileSummary=Label("ابتدا پوشهٔ دانلود را انتخاب کنید");layout.Controls.Add(fileSummary,0,2);
   var location=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};location.RowStyles.Add(new RowStyle(SizeType.Percent,100));location.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));location.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));
   destination.ReadOnly=true;destination.RightToLeft=RightToLeft.No;destination.AccessibleName="مسیر فایل";location.Controls.Add(Input(destination),0,0);
   browse=Button("انتخاب پوشه");browse.Dock=DockStyle.Fill;location.Controls.Add(browse,1,0);layout.Controls.Add(location,0,3);browse.Click+=(s,e)=>ChooseFolder();
   locationRow=location;
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Padding=new Padding(0,8,0,0)};
   start=Button("شروع دانلود");((GlassButton)start).Primary=true;start.Width=180;pause=Button("توقف");pause.Enabled=false;
   connections.Minimum=1;connections.Maximum=16;connections.Value=8;connections.Width=64;connections.AccessibleName="تعداد اتصال هم‌زمان";connections.Margin=new Padding(16,10,4,0);
   actions.Controls.Add(start);actions.Controls.Add(pause);actions.Controls.Add(connections);actions.Controls.Add(new Label{Text="اتصال هم‌زمان",AutoSize=true,Padding=new Padding(0,11,0,0),BackColor=Color.Transparent});layout.Controls.Add(actions,0,4);
   start.Click+=async(s,e)=>await Start();pause.Click+=(s,e)=>{engine.Pause();pause.Enabled=false;status.Text="در حال توقف…";};
   progress.Dock=DockStyle.Fill;progress.Maximum=1000;progress.Margin=Padding.Empty;layout.Controls.Add(progress,0,5);
   status=Label("برای شروع، لینک فایل را وارد کنید.");layout.Controls.Add(status,0,6);
   detail=Label("تا ۱۶ اتصال • توقف و ادامهٔ دانلود");detail.Font=Brand.Font(10);detail.RightToLeft=RightToLeft.No;detail.TextAlign=ContentAlignment.MiddleRight;layout.Controls.Add(detail,0,7);
   var historyPanel=new GlassPanel{Dock=DockStyle.Fill,Padding=new Padding(20,12,20,14),Margin=Padding.Empty};content.Controls.Add(historyPanel,0,4);
   var listLayout=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));listLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));historyPanel.Controls.Add(listLayout);
   var listTitle=Label("دانلودهای کامل‌شده     •     کلیک: باز کردن فایل     /     راست‌کلیک: نمایش در پوشه");listTitle.Font=Brand.Font(12,FontStyle.Bold);listLayout.Controls.Add(listTitle,0,0);
   history.Dock=DockStyle.Fill;history.View=View.Details;history.BorderStyle=BorderStyle.None;history.FullRowSelect=true;history.MultiSelect=false;history.HideSelection=false;history.AccessibleName="دانلودهای کامل‌شده";history.RightToLeft=RightToLeft.No;history.OwnerDraw=true;
   history.Columns.Add("نام فایل",245);history.Columns.Add("حجم",110);history.Columns.Add("تاریخ دانلود",155);history.Columns.Add("مدت دانلود",90);history.Columns.Add("سرعت متوسط",110);history.SizeChanged+=(s,e)=>{history.Columns[0].Width=Math.Max(150,history.ClientSize.Width-485);};var listHost=new Panel{Dock=DockStyle.Fill};listHost.Controls.Add(history);emptyHint=Label("هنوز فایلی دریافت نشده\nاولین دانلود کامل‌شده اینجا نمایش داده می‌شود.");emptyHint.TextAlign=ContentAlignment.MiddleCenter;emptyHint.Dock=DockStyle.Fill;listHost.Controls.Add(emptyHint);emptyHint.BringToFront();listLayout.Controls.Add(listHost,0,1);history.DrawColumnHeader+=(s,e)=>{using(var b=new SolidBrush(dark?Color.FromArgb(39,55,73):Color.FromArgb(244,245,250)))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,Brand.Font(10),e.Bounds,history.ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.Right|TextFormatFlags.RightToLeft);};
   history.DrawItem+=(s,e)=>{e.DrawDefault=false;};history.DrawSubItem+=(s,e)=>{using(var b=new SolidBrush(e.Item.Selected?(dark?Color.FromArgb(68,55,110):Color.FromArgb(238,229,253)):history.BackColor))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.SubItem.Text,history.Font,e.Bounds,history.ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|(e.ColumnIndex==0?TextFormatFlags.Left:TextFormatFlags.Right));};
   var menu=new ContextMenuStrip{Font=Brand.Font(12),RightToLeft=RightToLeft.Yes};menu.Items.Add("باز کردن فایل",null,(s,e)=>OpenCompleted(false));menu.Items.Add("نمایش در پوشه",null,(s,e)=>OpenCompleted(true));history.ContextMenuStrip=menu;
   history.MouseDown+=(s,e)=>{var hit=history.HitTest(e.Location);if(hit.Item!=null)hit.Item.Selected=true;else history.SelectedItems.Clear();};history.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left&&history.HitTest(e.Location).Item!=null)OpenCompleted(false);};history.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){OpenCompleted(false);e.Handled=true;}};menu.Opening+=(s,e)=>{e.Cancel=history.SelectedItems.Count==0;};
   content.Controls.Add(new AdPlacement(restore && settingsPath==null){Margin=new Padding(0,10,0,0)},0,5);dark=false;PaintTheme(this);
   Shown+=(s,e)=>{Brand.Cue(destination,"ابتدا پوشهٔ دانلود را انتخاب کنید");UpdateTarget();};
   if(restore)try{saved=Saved.Load(settings);saved.Save(settings);link.Text=saved.Url;connections.Value=Math.Max(1,Math.Min(16,saved.Connections));UpdateTarget();ReloadHistory();}catch{status.Text="تنظیمات قبلی خوانده نشد؛ پوشه را دوباره انتخاب کنید.";}
   RenderSpeed();
   if(restore && settingsPath==null){var reportsTimer=new Timer{Interval=60000};reportsTimer.Tick+=async(s,e)=>await ProblemReports.Flush();Shown+=async(s,e)=>await ProblemReports.Flush();reportsTimer.Start();FormClosed+=(s,e)=>reportsTimer.Dispose();}
   timer.Interval=500;timer.Tick+=(s,e)=>RefreshDownload();timer.Start();
   FormClosing+=async(s,e)=>{if(engine.Running&&!closing){e.Cancel=true;closing=true;engine.Pause();Enabled=false;while(engine.Running)await Task.Delay(100);Close();}};
  }
  Control Input(TextBox box){var panel=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(12,6,12,5),Margin=new Padding(0,2,6,4)};box.BorderStyle=BorderStyle.None;box.Dock=DockStyle.Fill;panel.Controls.Add(box);return panel;}
  Label Stat(TableLayoutPanel host,string title,string value,int column,Color tint){var panel=new GlassPanel{Dock=DockStyle.Fill,Radius=14,Padding=new Padding(14,3,14,3),Margin=new Padding(6,0,6,0),Tint=tint};var rows=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};rows.RowStyles.Add(new RowStyle(SizeType.Percent,65));rows.RowStyles.Add(new RowStyle(SizeType.Percent,35));var number=Label(value);number.Font=Brand.Font(20,FontStyle.Bold);number.TextAlign=ContentAlignment.MiddleCenter;rows.Controls.Add(number,0,0);var caption=Label(title);caption.Font=Brand.Font(9);caption.TextAlign=ContentAlignment.MiddleCenter;rows.Controls.Add(caption,0,1);panel.Controls.Add(rows);host.Controls.Add(panel,column,0);return number;}
  Label Label(string text) {return new Label {Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,BackColor=Color.Transparent,Margin=Padding.Empty,Font=Brand.Font(10)};}
  GlassButton Button(string text) {int icon=text=="دانلود جدید"?1:text=="فایل‌های دریافت‌شده"?2:text=="پوشهٔ دانلودها"?3:text=="تنظیم پوشه"?4:text=="تیره / روشن"?5:text=="گزارش مشکل"?6:text=="ویندوز و مرورگر"?7:0;return new GlassButton {Text=text,Glyph=icon,Font=Brand.Font(icon>0?10:12),Height=44,Width=110,Margin=new Padding(4,4,4,4)};}
  void PaintTheme(Control root) {
   GlassDark=dark;
   root.ForeColor=dark ? Color.FromArgb(233,235,250) : Color.FromArgb(45,40,63);
   if(root is TextBox || root is NumericUpDown || root is ListView)root.BackColor=dark?Color.FromArgb(33,38,59):Color.FromArgb(253,253,255);
   else if(root is Panel || root is Label || root is LogoView)root.BackColor=Color.Transparent;
   if(root is GlassPanel)((GlassPanel)root).Dark=dark;
   if(root is GlassButton)((GlassButton)root).Dark=dark;
   if(root is GlassProgress)((GlassProgress)root).Dark=dark;
   foreach(Control child in root.Controls) PaintTheme(child);
  }
  void UpdateTarget() {
   if(engine.Running)return;
   Uri uri;string url=link.Text.Trim();
   bool valid=Uri.TryCreate(url,UriKind.Absolute,out uri) && (uri.Scheme=="http" || uri.Scheme=="https");
   try{destination.Text=valid?saved.TargetFor(url):(saved.Folder ?? "");}catch{destination.Text=saved.Folder ?? "";}
   linkSummary.Text=valid?uri.Host+" / "+DownloadEngine.FileName(url):(String.IsNullOrEmpty(url)?"افزودن لینک دانلود…":"نمایش و ویرایش لینک…");
   bool configured=!String.IsNullOrEmpty(saved.Folder);
   locationRow.Visible=!configured;downloadLayout.RowStyles[3].Height=configured?0:50;
   contentLayout.RowStyles[2].Height=configured?292:342;
   fileSummary.Text=configured?(valid?"فایل: "+Path.GetFileName(destination.Text):"پوشهٔ دانلود آماده است؛ لینک را بچسبانید."):"ابتدا پوشهٔ دانلود را انتخاب کنید";
   hints.SetToolTip(fileSummary,configured?saved.Folder:"");
  }
  void EditLink() {
   using(var dialog=new Form{Text="لینک کامل دانلود",Size=new Size(720,260),MinimumSize=new Size(520,240),StartPosition=FormStartPosition.CenterParent,Font=Brand.Font(12),RightToLeft=RightToLeft.Yes,MinimizeBox=false,MaximizeBox=false}){
    var full=new TextBox{Text=link.Text,Multiline=true,ScrollBars=ScrollBars.Vertical,RightToLeft=RightToLeft.No,Dock=DockStyle.Fill,AccessibleName="لینک کامل دانلود"};
    var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(18),RowCount=3,ColumnCount=1};body.RowStyles.Add(new RowStyle(SizeType.Absolute,28));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Absolute,48));body.Controls.Add(Label("لینک کامل را ببینید یا ویرایش کنید"),0,0);body.Controls.Add(full,0,1);
    var save=new GlassButton{Text="تأیید لینک",Dock=DockStyle.Right,Width=130,DialogResult=DialogResult.OK,Primary=true};body.Controls.Add(save,0,2);dialog.Controls.Add(body);dialog.AcceptButton=save;
    if(dialog.ShowDialog(this)==DialogResult.OK)link.Text=full.Text.Trim();
   }
  }
  void ReloadHistory() {
   history.BeginUpdate();history.Items.Clear();
   foreach(var file in saved.History){var item=new ListViewItem(Path.GetFileName(file.Path));item.SubItems.Add((file.Size/1048576.0).ToString("0.00")+" MB");item.SubItems.Add(file.PersianDate());item.SubItems.Add(file.Duration());item.SubItems.Add(file.Average(saved.SpeedInBits));item.Tag=file;item.ToolTipText=file.Path+"\n"+file.PersianDate()+"\nمدت: "+file.Duration()+"\nسرعت متوسط: "+file.Average(saved.SpeedInBits);history.Items.Add(item);}
   history.ShowItemToolTips=true;history.EndUpdate();
   emptyHint.Visible=history.Items.Count==0;
   count.Text=saved.History.Count.ToString();long bytes=0;foreach(var file in saved.History)bytes+=file.Size;total.Text=(bytes/1048576.0).ToString("0.0")+" MB";
  }
  void OpenCompleted(bool reveal) {
   if(history.SelectedItems.Count==0)return;
   var file=(CompletedFile)history.SelectedItems[0].Tag;
   try{if(!File.Exists(file.Path)){MessageBox.Show("فایل جابه‌جا یا حذف شده است:\n"+file.Path,Text);return;}if(reveal)Process.Start("explorer.exe","/select,\""+file.Path+"\"");else Process.Start(new ProcessStartInfo(file.Path){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,Text);}
  }
  bool ChooseFolder() {
   using(var dlg=new FolderBrowserDialog {Description="پوشهٔ ذخیرهٔ همهٔ دانلودها را انتخاب کنید",SelectedPath=saved.Folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")}) {
    if(dlg.ShowDialog()!=DialogResult.OK)return false;
    try{saved.Folder=dlg.SelectedPath;saved.Save(settings);UpdateTarget();return true;}catch(Exception ex){MessageBox.Show(ex.Message,Text);return false;}
   }
  }
  async Task Start(bool confirmed=false) {
   if(engine.Running) return;
   Uri uri; string url=link.Text.Trim();
   if(!Uri.TryCreate(url,UriKind.Absolute,out uri) || (uri.Scheme!="http" && uri.Scheme!="https")) {MessageBox.Show("لینک مستقیم معتبر وارد کنید.",Text);return;}
   if(String.IsNullOrEmpty(saved.Folder) && !ChooseFolder()) return;
   UpdateTarget();
   if(!confirmed&&!ConfirmDownload(url,false))return;
   try {
    string target=Path.GetFullPath(destination.Text);
    if(File.Exists(target)) {MessageBox.Show("این فایل وجود دارد؛ نام تازه‌ای انتخاب کنید.",Text);return;}
    Directory.CreateDirectory(Path.GetDirectoryName(target));
    currentUrl=url;currentTarget=target;currentConnections=(int)connections.Value;
    saved.Url=url;saved.Target=target;saved.Connections=currentConnections;saved.Save(settings);
    previous=0;previousTime=DateTime.UtcNow;downloadSpeed.Reset();
    link.Enabled=linkSummary.Enabled=paste.Enabled=connections.Enabled=browse.Enabled=start.Enabled=false;pause.Enabled=true;
    await engine.Run(url,target,currentConnections);
    if(engine.Error==null && File.Exists(target)){saved.Complete(target,engine.State);saved.Save(settings);ReloadHistory();}
    if(!closing && engine.Error!=null) MessageBox.Show(engine.Error, "مشکل دانلود",MessageBoxButtons.OK,MessageBoxIcon.Warning);
   } catch(Exception ex) {MessageBox.Show(ex.Message,Text);}
   finally {link.Enabled=linkSummary.Enabled=paste.Enabled=connections.Enabled=browse.Enabled=start.Enabled=true;pause.Enabled=false;RefreshDownload();if(!closing&&File.Exists(currentTarget??"")&&saved.Queue.Count>0){string next=saved.Queue[0];saved.Queue.RemoveAt(0);SaveSettings();BeginInvoke(new Action(()=>ReceiveDownload(next,true)));}}
  }
  void SaveSettings(){saved.Url=link.Text;saved.Target=destination.Text;saved.Connections=(int)connections.Value;saved.Save(settings);}
  void ConfigureSpeedToggle(Control control){control.Cursor=Cursors.Hand;control.Click+=(s,e)=>{saved.SpeedInBits=!saved.SpeedInBits;RenderSpeed();ReloadHistory();try{saved.Save(settings);}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};foreach(Control child in control.Controls)ConfigureSpeedToggle(child);}
  void RenderSpeed(){double value=displayedRate*(saved.SpeedInBits?8:1)/1000000d;speed.Text=(value>0?value.ToString("0.00"):"0")+(saved.SpeedInBits?" Mb/s":" MB/s");speed.AccessibleName=saved.SpeedInBits?"سرعت دانلود به مگابیت بر ثانیه":"سرعت دانلود به مگابایت بر ثانیه";hints.SetToolTip(speed,saved.SpeedInBits?"مگابیت بر ثانیه؛ کلیک برای نمایش مگابایت":"مگابایت بر ثانیه؛ کلیک برای نمایش مگابیت");}
  internal bool ReceiveDownload(string url,bool confirmed=false){
   LastDownloadDeclined=false;
   if(!Integration.ValidUrl(url)||closing)return false;
   if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Show();Activate();
   if(!confirmed&&!ConfirmDownload(url,engine.Running)){LastDownloadDeclined=true;return false;}
   if(engine.Running){if(saved.Queue.Count>=100)return false;if(!saved.Queue.Contains(url)&&url!=currentUrl){saved.Queue.Add(url);SaveSettings();}status.Text="در صف دانلود: "+saved.Queue.Count;return true;}
   if(String.IsNullOrEmpty(saved.Folder)){saved.Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");}
   link.Text=url;SaveSettings();BeginInvoke(new Action(async()=>await Start(true)));return true;
  }
  bool ConfirmDownload(string url,bool queued){using(var dialog=new DownloadPrompt(url,DownloadEngine.FileName(url),queued,dark))return dialog.ShowDialog(this)==DialogResult.Yes;}
  void IntegrationSettings(){using(var dialog=new GlassForm{Text="ویندوز و مرورگر",ClientSize=new Size(590,390),StartPosition=FormStartPosition.CenterParent,Font=Brand.Font(12),RightToLeft=RightToLeft.Yes}){
   var layout=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(24),RowCount=4};foreach(int height in new[]{48,130,55,55})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
   var startup=new CheckBox{Text="اجرای آسان دانلود با روشن‌شدن کامپیوتر",Checked=Integration.StartupEnabled,Dock=DockStyle.Fill};startup.CheckedChanged+=(s,e)=>{try{Integration.StartupEnabled=startup.Checked;}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};layout.Controls.Add(startup,0,0);
   layout.Controls.Add(Label("برای دریافت دانلودهای کروم یا اج:\n۱. صفحهٔ افزونه‌ها را باز کنید و Developer mode را روشن کنید.\n۲. Load unpacked را بزنید و پوشهٔ افزونهٔ آسان دانلود را انتخاب کنید.\nدانلودهای نیازمند ورود به سایت، در مرورگر ادامه می‌یابند."),0,1);
   var folderButton=Button("پوشهٔ افزونه");folderButton.Width=200;folderButton.Click+=(s,e)=>Process.Start("explorer.exe","\""+Path.Combine(Path.GetDirectoryName(ReleaseInfo.InstalledPath),"BrowserExtension")+"\"");layout.Controls.Add(folderButton,0,2);
   var close=Button("بستن");close.DialogResult=DialogResult.OK;layout.Controls.Add(close,0,3);dialog.Controls.Add(layout);dialog.ShowDialog(this);
  }}
  void RefreshDownload() {
   if(engine.State==null) {if(engine.Running)status.Text=engine.Status;displayedRate=0;RenderSpeed();return;}
   long received=System.Threading.Interlocked.Read(ref engine.Received);
   double rate=downloadSpeed.Measure(System.Threading.Interlocked.Read(ref engine.SessionReceived));
   displayedRate=engine.Running?rate:0;RenderSpeed();
   previous=received;previousTime=DateTime.UtcNow;
   long size=engine.State.Size;
   progress.Style=size>0 ? ProgressBarStyle.Continuous : (engine.Running ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous);
   if(size>0) progress.Value=(int)Math.Min(1000,received*1000.0/size);
   progress.Invalidate();
   status.Text=engine.Status;
   detail.Text=(received/1048576.0).ToString("0.0")+" MB"+(size>0 ? " / "+(size/1048576.0).ToString("0.0")+" MB" : "")+(engine.Running?" • "+engine.ActiveConnections+" اتصال فعال":"")+(engine.Running && size>0 && rate>0 ? "   •   حدود "+TimeSpan.FromSeconds(Math.Min(8640000,Math.Max(0,size-received)/rate)).ToString(@"hh\:mm\:ss")+" باقی‌مانده" : "");
   if(!engine.Running) start.Text=File.Exists(currentTarget ?? "") ? "دانلود تازه" : "شروع / ادامه";
  }
 }
 static class Program {
  [STAThread] static void Main(string[] args) {
   ServicePointManager.DefaultConnectionLimit=32;
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   try {
    if(args.Length>0 && args[0]=="--install") {Install();return;}
    if(args.Length>1 && args[0]=="--screenshot") {
     using(var form=new Window(false)) {form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(args[1]);}form.Close();}return;
    }
    if(args.Length>0 && args[0]=="--apply-update"){UpdateClient.Apply(args);return;}
    using(var instance=new System.Threading.Mutex(false,"Local\\AsanDownload.Desktop.v1")){
     bool owned=false;try{try{owned=instance.WaitOne(0);}catch(System.Threading.AbandonedMutexException){owned=true;}
      if(!owned){Integration.Deliver("activate");return;}
      if(!(args.Length>0&&args[0]=="--skip-update"))using(var update=new StartupUpdate()){update.ShowDialog();if(update.Relaunching||update.ExitRequested)return;}
      var window=new Window(true);window.Shown+=(s,e)=>Integration.Listen(window);Application.Run(window);
     }finally{if(owned)instance.ReleaseMutex();}
    }
   } catch(Exception ex) {
    if(args.Length>0&&args[0]=="--apply-update"){try{if(File.Exists(ReleaseInfo.InstalledPath))Process.Start(new ProcessStartInfo(ReleaseInfo.InstalledPath,"--skip-update"){UseShellExecute=true});}catch{}Environment.ExitCode=1;return;}
    if(Environment.GetEnvironmentVariable("MEHR_INSTALL_SILENT")=="1")File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),"InstallError.txt"),ex.ToString());
    else MessageBox.Show(ex.Message,"دانلود");Environment.ExitCode=1;
   }
  }
  internal static void Install(bool copy=true) {
   string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MehrDownload");
   Directory.CreateDirectory(dir);
   string exe=Path.Combine(dir,"MehrDownload.exe");
   if(copy&&!String.Equals(Application.ExecutablePath,exe,StringComparison.OrdinalIgnoreCase)) File.Copy(Application.ExecutablePath,exe,true);
   Integration.RegisterBrowser();
   using(var preferences=Registry.CurrentUser.CreateSubKey(@"Software\MehrDownload")){Integration.StartupEnabled=true;preferences.SetValue("StartupConfigured",1);}
   Type t=Type.GetTypeFromProgID("WScript.Shell");dynamic shell=Activator.CreateInstance(t);
   foreach(string path in new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.Programs)}) {
    string old=Path.Combine(path,"Mehr Download.lnk");if(File.Exists(old))File.Delete(old);
    string previous=Path.Combine(path,"دانلود.lnk");if(File.Exists(previous))File.Delete(previous);
    string temporary=Path.Combine(path,"Download.lnk"),final=Path.Combine(path,"آسان دانلود.lnk");
    dynamic shortcut=shell.CreateShortcut(temporary);shortcut.TargetPath=exe;shortcut.IconLocation=exe+",0";shortcut.WorkingDirectory=dir;shortcut.Description="Asan Download";shortcut.Save();
    if(File.Exists(final))File.Delete(final);File.Move(temporary,final);
   }
   string uninstall=Path.Combine(dir,"Uninstall.ps1");
   File.WriteAllText(uninstall,"$ErrorActionPreference = 'Stop'\r\nStop-Process -Name MehrDownload -ErrorAction SilentlyContinue\r\n$target = Join-Path $env:LOCALAPPDATA 'Programs\\MehrDownload'\r\nif ([IO.Path]::GetFullPath($target) -ne [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs\\MehrDownload'))) { throw 'Invalid path' }\r\nRemove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'آسان دانلود.lnk') -ErrorAction SilentlyContinue\r\nRemove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'آسان دانلود.lnk') -ErrorAction SilentlyContinue\r\nRemove-Item -LiteralPath 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MehrDownload' -ErrorAction SilentlyContinue\r\nRemove-Item -LiteralPath $target -Recurse -Force\r\n",System.Text.Encoding.UTF8);
   using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MehrDownload")) {
    key.SetValue("DisplayName","آسان دانلود");key.SetValue("DisplayVersion",ReleaseInfo.Version);key.SetValue("Publisher","MehrNoor");key.SetValue("InstallLocation",dir);key.SetValue("DisplayIcon",exe);
    key.SetValue("UninstallString","powershell.exe -NoProfile -ExecutionPolicy Bypass -File \""+uninstall+"\"");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);
   }
   if(copy&&Environment.GetEnvironmentVariable("MEHR_INSTALL_SILENT")!="1") Process.Start(exe);
  }
 }
}
