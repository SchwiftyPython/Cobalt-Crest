using UnityEngine; using UnityEngine.UI;
public class PerfOverlay : MonoBehaviour {
  Text _text; float _accum; int _frames;
  void Start(){
    var canvas = Object.FindObjectOfType<Canvas>(); if(canvas==null) return;
    var go=new GameObject("PerfText"); go.transform.SetParent(canvas.transform,false);
    _text=go.AddComponent<Text>(); _text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if(_text.font==null) _text.font=Resources.GetBuiltinResource<Font>("Arial.ttf");
    _text.fontSize=14; _text.alignment=TextAnchor.UpperLeft; _text.color=new Color(1,1,1,0.85f);
    var rt=go.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(10,-10); rt.sizeDelta=new Vector2(400,30);
  }
  void Update(){
    _accum+=Time.unscaledDeltaTime; _frames++; if(_accum>=0.5f && _text!=null){ float fps=_frames/_accum;
      int p=EcosystemManager.Instance?.GetCount("Producers")??0; int h=EcosystemManager.Instance?.GetCount("Herbivores")??0; int c=EcosystemManager.Instance?.GetCount("Carnivores")??0;
      _text.text=$"FPS: {fps:0.0}  P:{p} H:{h} C:{c}"; _accum=0f; _frames=0; }
  }
}
