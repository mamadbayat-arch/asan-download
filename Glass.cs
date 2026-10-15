using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MehrDownload {
 static class Brand {
  static PrivateFontCollection fonts=new PrivateFontCollection();
  static bool loaded;
  [DllImport("gdi32.dll")] static extern IntPtr AddFontMemResourceEx(IntPtr data,uint size,IntPtr reserved,ref uint count);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,string text);
  public static void Cue(TextBox box,string text){SendMessage(box.Handle,0x1501,new IntPtr(1),text);}
  public static Font Font(float size,FontStyle style=FontStyle.Regular) {
   if(!loaded){
    foreach(string name in new[]{"Kalameh-Regular.ttf","Kalameh-Bold.ttf"})using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){
     byte[] bytes=new byte[stream.Length];stream.Read(bytes,0,bytes.Length);
     IntPtr ptr=Marshal.AllocCoTaskMem(bytes.Length);Marshal.Copy(bytes,0,ptr,bytes.Length);uint count=0;AddFontMemResourceEx(ptr,(uint)bytes.Length,IntPtr.Zero,ref count);fonts.AddMemoryFont(ptr,bytes.Length);Marshal.FreeCoTaskMem(ptr);
    }loaded=true;
   }
   return new Font(fonts.Families[0],size,style);
  }
  public static GraphicsPath Round(RectangleF r,float radius) {
   var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
   p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
  }
  public static void Logo(Graphics g,Rectangle r) {
   g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var p=Round(r,r.Width*.28f))using(var b=new LinearGradientBrush(r,Color.FromArgb(25,146,120),Color.FromArgb(23,34,54),60f)){g.FillPath(b,p);using(var pen=new Pen(Color.FromArgb(170,255,255,255),Math.Max(1,r.Width/80f)))g.DrawPath(pen,p);}
   var old=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/100f,r.Height/100f);
   using(var p=new GraphicsPath()){
    p.AddLines(new[]{new PointF(43,21),new PointF(57,21),new PointF(57,49),new PointF(69,39),new PointF(78,50),new PointF(50,74),new PointF(22,50),new PointF(31,39),new PointF(43,49)});p.CloseFigure();using(var b=new SolidBrush(Color.White))g.FillPath(b,p);
   }
   using(var pen=new Pen(Color.FromArgb(210,255,255,255),6)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLine(pen,27,83,73,83);}
   g.Restore(old);
  }
 }
 class LogoView : Control {
  public LogoView(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;Size=new Size(68,68);AccessibleName="لوگوی دانلود";}
  protected override void OnPaint(PaintEventArgs e){Brand.Logo(e.Graphics,new Rectangle(3,3,Width-6,Height-6));}
 }
 class GlassForm : Form {
  public bool GlassDark;
  TitleBar titleBar;
  Bitmap background;Size backgroundSize;bool backgroundDark;
  public GlassForm(){DoubleBuffered=true;ResizeRedraw=true;FormBorderStyle=FormBorderStyle.None;Padding=new Padding(2,56,2,2);titleBar=new TitleBar(this);Controls.Add(titleBar);}
  protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(titleBar!=null){titleBar.Bounds=new Rectangle(8,6,Math.Max(1,ClientSize.Width-16),44);titleBar.BringToFront();titleBar.Invalidate();}}
  protected override void OnResize(EventArgs e){base.OnResize(e);if(titleBar!=null)titleBar.UpdateRestore();}
  internal void ToggleMaximized(){MaximizedBounds=Screen.FromControl(this).WorkingArea;WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
  protected override void WndProc(ref Message m){
   const int hitTest=0x84;
   if(m.Msg==hitTest && WindowState==FormWindowState.Normal){
    long position=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(position&0xffff),(short)((position>>16)&0xffff)));int edge=7;bool left=p.X<edge,right=p.X>=ClientSize.Width-edge,top=p.Y<edge,bottom=p.Y>=ClientSize.Height-edge;
    int hit=left?(top?13:bottom?16:10):right?(top?14:bottom?17:11):top?12:bottom?15:0;
    if(hit!=0){m.Result=new IntPtr(hit);return;}
   }
   base.WndProc(ref m);
  }
  protected override void OnPaintBackground(PaintEventArgs e){
   if(background==null||backgroundSize!=ClientSize||backgroundDark!=GlassDark){if(background!=null)background.Dispose();background=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));backgroundSize=ClientSize;backgroundDark=GlassDark;using(var canvas=Graphics.FromImage(background))PaintBackdrop(canvas);}
   e.Graphics.DrawImageUnscaled(background,0,0);
  }
  protected override void Dispose(bool disposing){if(disposing&&background!=null){background.Dispose();background=null;}base.Dispose(disposing);}
  void PaintBackdrop(Graphics g){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var b=new LinearGradientBrush(ClientRectangle,GlassDark?Color.FromArgb(22,25,43):Color.FromArgb(249,249,253),GlassDark?Color.FromArgb(29,32,50):Color.FromArgb(239,242,250),40f))g.FillRectangle(b,ClientRectangle);
   Glow(g,new Rectangle(Width-440,-240,640,700),GlassDark?Color.FromArgb(45,120,93,198):Color.FromArgb(45,178,147,242));
   Glow(g,new Rectangle(-280,Height-480,680,680),GlassDark?Color.FromArgb(40,57,142,157):Color.FromArgb(32,92,192,184));
  }
  void Glow(Graphics g,Rectangle r,Color c){using(var p=new GraphicsPath()){p.AddEllipse(r);using(var b=new PathGradientBrush(p)){b.CenterColor=c;b.SurroundColors=new[]{Color.FromArgb(0,c)};g.FillPath(b,p);}}}
 }
 class SmoothTableLayout : TableLayoutPanel {public SmoothTableLayout(){DoubleBuffered=true;}}
 class WindowActionButton : GlassButton {
  public new int Glyph;public bool Restored;bool over;
  public WindowActionButton(int glyph){Glyph=glyph;RightToLeft=RightToLeft.No;TabStop=true;Size=new Size(44,36);}
  protected override void OnMouseEnter(EventArgs e){over=true;base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){over=false;base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   Color ink=Dark?Color.FromArgb(128,224,198):Color.FromArgb(23,111,96);
   Color fill=Dark?Color.FromArgb(42,61,72):Color.FromArgb(232,244,240);
   if(over){fill=Glyph==2?Color.FromArgb(245,222,221):(Dark?Color.FromArgb(49,91,88):Color.FromArgb(205,234,224));if(Glyph==2)ink=Color.FromArgb(172,61,65);}
   using(var path=Brand.Round(new Rectangle(1,1,Width-3,Height-3),10))using(var brush=new SolidBrush(fill))g.FillPath(brush,path);
   float x=Width/2f,y=Height/2f;
   using(var pen=new Pen(ink,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;
    if(Glyph==0)g.DrawLine(pen,x-6,y+3,x+6,y+3);
    else if(Glyph==2){g.DrawLine(pen,x-5,y-5,x+5,y+5);g.DrawLine(pen,x+5,y-5,x-5,y+5);}
    else if(Restored){g.DrawLines(pen,new[]{new PointF(x-3,y-6),new PointF(x+6,y-6),new PointF(x+6,y+3)});g.DrawRectangle(pen,x-6,y-3,9,9);}
    else g.DrawRectangle(pen,x-5,y-5,10,10);
    if(Focused&&ShowFocusCues)g.DrawRectangle(pen,5,5,Width-11,Height-11);
   }
  }
 }
 class TitleBar : Panel {
  GlassForm owner;WindowActionButton minimize,maximize,close;
  [DllImport("user32.dll")]static extern bool ReleaseCapture();
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,IntPtr lparam);
  public TitleBar(GlassForm form){
   owner=form;RightToLeft=RightToLeft.No;SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;
   minimize=new WindowActionButton(0){AccessibleName="کوچک کردن پنجره"};maximize=new WindowActionButton(1){AccessibleName="بزرگ کردن پنجره"};close=new WindowActionButton(2){AccessibleName="بستن برنامه"};Controls.Add(minimize);Controls.Add(maximize);Controls.Add(close);
   minimize.Click+=(s,e)=>owner.WindowState=FormWindowState.Minimized;maximize.Click+=(s,e)=>Toggle();close.Click+=(s,e)=>owner.Close();
   MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(owner.Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};MouseDoubleClick+=(s,e)=>{if(e.Button==MouseButtons.Left)Toggle();};
  }
  void Toggle(){owner.ToggleMaximized();}
  public void UpdateRestore(){maximize.Restored=owner.WindowState==FormWindowState.Maximized;maximize.AccessibleName=maximize.Restored?"بازگرداندن اندازهٔ پنجره":"بزرگ کردن پنجره";maximize.Invalidate();}
  protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(close==null)return;close.Location=new Point(Width-48,4);maximize.Location=new Point(Width-98,4);minimize.Location=new Point(Width-148,4);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Brand.Logo(g,new Rectangle(12,9,26,26));var rect=new Rectangle(48,0,Math.Max(1,Width-210),Height);TextRenderer.DrawText(g,"آسان دانلود",Brand.Font(11,FontStyle.Bold),rect,owner.GlassDark?Color.FromArgb(225,243,237):Color.FromArgb(26,51,61),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.RightToLeft);using(var pen=new Pen(owner.GlassDark?Color.FromArgb(65,110,157,151):Color.FromArgb(160,207,225,223)))g.DrawLine(pen,10,Height-1,Width-10,Height-1);}
 }
 class GlassPanel : Panel {
  public bool Dark; public int Radius=24;public Color Tint=Color.Empty;
  public GlassPanel(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;ResizeRedraw=true;Padding=new Padding(22);}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var r=new Rectangle(1,1,Width-3,Height-3);if(r.Width<1||r.Height<1)return;
   using(var p=Brand.Round(r,Radius)){
    using(var b=new LinearGradientBrush(r,Dark?Color.FromArgb(170,48,52,73):(Tint.IsEmpty?Color.FromArgb(224,255,255,255):Tint),Dark?Color.FromArgb(155,29,34,54):(Tint.IsEmpty?Color.FromArgb(188,255,255,255):Color.FromArgb(205,Tint)),75f))g.FillPath(b,p);
    using(var pen=new Pen(Dark?Color.FromArgb(95,191,230,245):Color.FromArgb(235,255,255,255),1.3f))g.DrawPath(pen,p);
   }
  }
 }
 class GlassButton : Control,IButtonControl {
  public bool Dark,Primary;public int Glyph;bool hover,pressed;
  public DialogResult DialogResult {get;set;}
  public GlassButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor|ControlStyles.Selectable|ControlStyles.StandardClick,true);BackColor=Color.Transparent;Cursor=Cursors.Hand;Font=Brand.Font(12);TabStop=true;RightToLeft=RightToLeft.No;AccessibleRole=AccessibleRole.PushButton;ResizeRedraw=true;}
  public void NotifyDefault(bool value){Invalidate();}
  public void PerformClick(){if(Enabled)OnClick(EventArgs.Empty);}
  protected override void OnClick(EventArgs e){base.OnClick(e);var form=FindForm();if(form!=null&&DialogResult!=DialogResult.None)form.DialogResult=DialogResult;}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){pressed=false;base.OnLostFocus(e);Invalidate();}
  protected override bool IsInputKey(Keys key){return key==Keys.Space||key==Keys.Enter||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){pressed=true;Invalidate();e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnKeyUp(KeyEventArgs e){if(pressed&&(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter)){pressed=false;PerformClick();Invalidate();e.Handled=true;}base.OnKeyUp(e);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var r=new Rectangle(2,2,Width-5,Height-5);if(r.Width<1||r.Height<1)return;
   Color top,bottom;
   if(Primary){top=Color.FromArgb(20,125,104);bottom=top;}else{top=Dark?Color.FromArgb(44,50,68):Color.FromArgb(244,247,249);bottom=top;}
   if(hover){top=ControlPaint.Light(top,.1f);bottom=ControlPaint.Light(bottom,.08f);}if(pressed){top=ControlPaint.Dark(top,.1f);bottom=ControlPaint.Dark(bottom,.1f);}
   if(!Enabled){top=Dark?Color.FromArgb(38,54,67):Color.FromArgb(219,228,229);bottom=top;}
   using(var p=Brand.Round(r,15)){
    if(Primary||hover||pressed)using(var b=new SolidBrush(top))g.FillPath(b,p);
    if(Focused && ShowFocusCues){var focus=new Rectangle(7,7,Width-15,Height-15);ControlPaint.DrawFocusRectangle(g,focus,ForeColor,top);}
   }
   int textInset=Glyph==0?12:26;
   var textBounds=new Rectangle(textInset,4,Math.Max(1,Width-2*textInset),Height-4);
   TextRenderer.DrawText(g,Text,Font,textBounds,!Enabled?(Dark?Color.FromArgb(157,175,188):Color.FromArgb(102,118,124)):(Primary?Color.White:ForeColor),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding|(RightToLeft==RightToLeft.Yes?TextFormatFlags.RightToLeft:0));
   if(Glyph!=0){float x=Width-23,y=Height/2f;using(var pen=new Pen(Primary?Color.White:(Dark?Color.FromArgb(139,215,195):Color.FromArgb(34,123,107)),1.6f)){pen.StartCap=pen.EndCap=LineCap.Round;
    if(Glyph==1){g.DrawLine(pen,x-7,y,x+7,y);g.DrawLine(pen,x,y-7,x,y+7);}
    else if(Glyph==2){g.DrawRectangle(pen,x-7,y-7,14,14);g.DrawLine(pen,x-4,y-2,x+4,y-2);g.DrawLine(pen,x-4,y+3,x+2,y+3);}
    else if(Glyph==3){g.DrawLines(pen,new[]{new PointF(x-8,y+6),new PointF(x-8,y-6),new PointF(x-1,y-6),new PointF(x+2,y-3),new PointF(x+8,y-3),new PointF(x+8,y+6),new PointF(x-8,y+6)});}
    else if(Glyph==4){g.DrawLine(pen,x-7,y-4,x+7,y-4);g.DrawLine(pen,x-7,y+4,x+7,y+4);g.DrawEllipse(pen,x-4,y-7,5,5);g.DrawEllipse(pen,x+1,y+1,5,5);}
    else if(Glyph==5){g.DrawEllipse(pen,x-7,y-7,14,14);g.DrawArc(pen,x-4,y-7,8,14,90,180);}
    else if(Glyph==6){g.DrawRectangle(pen,x-7,y-7,14,12);g.DrawLine(pen,x-3,y+5,x-6,y+9);g.DrawLine(pen,x,y-4,x,y);g.DrawEllipse(pen,x-.5f,y+2,1,1);}
    else if(Glyph==7){g.DrawRectangle(pen,x-8,y-6,16,11);g.DrawLine(pen,x,y+5,x,y+8);g.DrawLine(pen,x-4,y+8,x+4,y+8);}
   }}
  }
 }
 class GlassProgress : Control {
  public bool Dark;
  public int Maximum=1000;public int Value;public ProgressBarStyle Style;
  public GlassProgress(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;TabStop=false;ResizeRedraw=true;}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var r=new Rectangle(0,3,Width,Math.Max(5,Height-6));
   using(var p=Brand.Round(r,8))using(var b=new SolidBrush(Dark?Color.FromArgb(37,64,81):Color.FromArgb(203,223,229)))g.FillPath(b,p);
   int width=Style==ProgressBarStyle.Marquee ? Width/4 : (int)(Width*(Value/(double)Math.Max(1,Maximum)));
   if(width>1){var fill=new Rectangle(Width-width,3,width,r.Height);using(var p=Brand.Round(fill,8))using(var b=new LinearGradientBrush(fill,Color.FromArgb(178,136,244),Color.FromArgb(114,76,193),0f))g.FillPath(b,p);}
  }
 }
}
