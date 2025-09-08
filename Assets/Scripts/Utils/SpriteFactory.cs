using UnityEngine;
public static class SpriteFactory {
  public static Sprite CreateDiscSprite(Color color, int size=16){
    size=Mathf.Clamp(size,8,128);
    var tex=new Texture2D(size,size,TextureFormat.ARGB32,false);
    var c=new Color32((byte)(color.r*255),(byte)(color.g*255),(byte)(color.b*255),255);
    var clear=new Color32(0,0,0,0); int r=size/2; int r2=r*r;
    for(int y=0;y<size;y++) for(int x=0;x<size;x++){ int dx=x-r, dy=y-r; tex.SetPixel(x,y,(dx*dx+dy*dy<=r2)?c:clear); }
    tex.Apply(false); return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(0.5f,0.5f),100f);
  }
}
