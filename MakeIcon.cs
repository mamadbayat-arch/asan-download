using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using MehrDownload;
class MakeIcon {
 static void Main(){
  using(var icon=new FileStream("Assets/Download.ico",FileMode.Create))using(var writer=new BinaryWriter(icon)){
   int[] sizes={16,24,32,48,64,128,256};var frames=new byte[sizes.Length][];
   for(int i=0;i<sizes.Length;i++)using(var bitmap=new Bitmap(sizes[i],sizes[i]))using(var g=Graphics.FromImage(bitmap))using(var memory=new MemoryStream()){
    Brand.Logo(g,new Rectangle(1,1,sizes[i]-2,sizes[i]-2));bitmap.Save(memory,ImageFormat.Png);frames[i]=memory.ToArray();if(sizes[i]==256)bitmap.Save("Assets/Logo.png",ImageFormat.Png);
   }
   writer.Write((short)0);writer.Write((short)1);writer.Write((short)sizes.Length);int offset=6+16*sizes.Length;
   for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((short)0);writer.Write((short)1);writer.Write((short)32);writer.Write(frames[i].Length);writer.Write(offset);offset+=frames[i].Length;}
   foreach(var frame in frames)writer.Write(frame);
  }
 }
}
