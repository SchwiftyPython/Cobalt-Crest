using Managers;
using UnityEngine; using UnityEngine.UI;
[RequireComponent(typeof(RawImage))] public class PopulationGraph:MonoBehaviour{
  public int width=460; public float sampleInterval=0.25f; [Range(0.01f,1f)] public float autoscaleLerp=0.2f;
  public Color backgroundColor=new Color(0.09f,0.09f,0.09f,1f), gridColor=new Color(1f,1f,1f,0.05f); public int gridYLines=4;
  RawImage _raw; Texture2D _tex; float _timer; int _cursorX,_height; int[] _lastY=new int[3]; float _displayMax=10f;
  void Awake(){
    _raw=GetComponent<RawImage>(); var rt=(RectTransform)transform; _height=Mathf.Max(120, Mathf.RoundToInt(rt.rect.height)); if(width<64)
    {
      width=64;
    }

    _tex=new Texture2D(width,_height,TextureFormat.RGBA32,false,false); _tex.wrapMode=TextureWrapMode.Clamp; _tex.filterMode=FilterMode.Point;
    var fill=new Color32[width*_height]; for(var i=0;i<fill.Length;i++) fill[i]=backgroundColor; _tex.SetPixels32(fill); _tex.Apply(); _raw.texture=_tex;
    for(var i=0;i<_lastY.Length;i++) _lastY[i]=-1; DrawGrid();
  }
  void Update(){
    _timer+=Time.unscaledDeltaTime; if(_timer<sampleInterval)
    {
      return;
    }

    _timer=0f;
    int prod=SafeCount("Producers"), herb=SafeCount("Herbivores"), carn=SafeCount("Carnivores"); var currentMax=Mathf.Max(1,prod,herb,carn);
    _displayMax=Mathf.Lerp(_displayMax, Mathf.Max(currentMax,10), autoscaleLerp);
    Advance(); DrawCol(_cursorX);
    Plot(0, prod, ConfigService.Instance? ConfigService.Instance.GetSpeciesColor(SpeciesId.Producer, Color.green): Color.green);
    Plot(1, herb, ConfigService.Instance? ConfigService.Instance.GetSpeciesColor(SpeciesId.Herbivore, Color.cyan): Color.cyan);
    Plot(2, carn, ConfigService.Instance? ConfigService.Instance.GetSpeciesColor(SpeciesId.Carnivore, Color.red): Color.red);
    _tex.Apply(false);
  }
  int SafeCount(string id){ return EcosystemManager.Instance==null?0: EcosystemManager.Instance.GetCount(id); }
  void Advance(){ _cursorX = (_cursorX+1)%width; }
  void DrawCol(int x){ for(var y=0;y<_height;y++) _tex.SetPixel(x,y,backgroundColor); if(_cursorX%80==0)
    {
      for(var y=0;y<_height;y++) _tex.SetPixel(x,y,gridColor);
    }
  }
  void DrawGrid(){ if(gridYLines<=0)
    {
      return;
    }

    for(var i=1;i<=gridYLines;i++){ var y=Mathf.RoundToInt((i/(float)(gridYLines+1))*(_height-1)); for(var x=0;x<width;x++) _tex.SetPixel(x,y,gridColor);} _tex.Apply(false); }
  void Plot(int idx,int count, Color color){ var norm=Mathf.Clamp01(count/Mathf.Max(1f,_displayMax)); var y=Mathf.Clamp(Mathf.RoundToInt(norm*(_height-1)),0,_height-1); var last=_lastY[idx]; if(last<0)
    {
      last=y;
    }

    int y0=Mathf.Min(last,y), y1=Mathf.Max(last,y); for(var yy=y0;yy<=y1;yy++) _tex.SetPixel(_cursorX,yy,color); _lastY[idx]=y; }
}
