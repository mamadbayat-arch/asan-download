using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
namespace MehrDownload {
 class DownloadPrompt:GlassForm {
  public DownloadPrompt(string url,string name,bool queued,bool dark){
   Text="تأیید دانلود";GlassDark=dark;Font=Brand.Font(12);ClientSize=new Size(570,340);StartPosition=FormStartPosition.CenterParent;RightToLeft=RightToLeft.Yes;
   var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(26),ColumnCount=1,RowCount=4};body.RowStyles.Add(new RowStyle(SizeType.Absolute,42));body.RowStyles.Add(new RowStyle(SizeType.Absolute,86));body.RowStyles.Add(new RowStyle(SizeType.Absolute,40));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(body);
   Color ink=dark?Color.White:Color.FromArgb(23,34,54);body.ForeColor=ink;body.BackColor=Color.Transparent;
   body.Controls.Add(new Label{Text=queued?"این فایل به صف دانلود اضافه شود؟":"این فایل را دانلود می‌کنید؟",Font=Brand.Font(17,FontStyle.Bold),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,BackColor=Color.Transparent},0,0);
   body.Controls.Add(new Label{Text=name,Dock=DockStyle.Fill,Font=Brand.Font(14,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=true,BackColor=Color.Transparent,RightToLeft=RightToLeft.No},0,1);
   body.Controls.Add(new Label{Text=new Uri(url).Host,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Transparent,RightToLeft=RightToLeft.No},0,2);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,BackColor=Color.Transparent,Padding=new Padding(0,12,0,0)};
   var yes=new GlassButton{Text=queued?"افزودن به صف":"دانلود کن",Primary=true,Dark=dark,Size=new Size(160,44),DialogResult=DialogResult.Yes};var no=new GlassButton{Text="انصراف",Dark=dark,ForeColor=ink,Size=new Size(130,44),DialogResult=DialogResult.Cancel};actions.Controls.Add(yes);actions.Controls.Add(no);body.Controls.Add(actions,0,3);CancelButton=no;Shown+=(s,e)=>no.Focus();
   var timeout=new Timer{Interval=90000};timeout.Tick+=(s,e)=>{DialogResult=DialogResult.Cancel;Close();};Shown+=(s,e)=>timeout.Start();FormClosed+=(s,e)=>timeout.Dispose();
  }
 }
 class DownloadSpeed {
  class Sample {public long Bytes;public double Seconds;}
  readonly Queue<Sample> samples=new Queue<Sample>();readonly Stopwatch clock=new Stopwatch();
  public void Reset(){samples.Clear();clock.Restart();samples.Enqueue(new Sample{Seconds=0,Bytes=0});}
  public double Measure(long bytes){if(!clock.IsRunning)Reset();var now=new Sample{Seconds=clock.Elapsed.TotalSeconds,Bytes=bytes};samples.Enqueue(now);while(samples.Count>1&&now.Seconds-samples.Peek().Seconds>3)samples.Dequeue();var first=samples.Peek();double elapsed=now.Seconds-first.Seconds;return elapsed>=.2?Math.Max(0,now.Bytes-first.Bytes)/elapsed:0;}
 }
}
