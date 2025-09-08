using System.Collections.Generic;
using Entities;
using UnityEngine;
public class SpatialIndex : MonoBehaviour {
  public static SpatialIndex Instance { get; private set; }
  private SpatialHash2D<Producer> _producers; private SpatialHash2D<Herbivore> _herbivores;
  private readonly List<Producer> _pBuf=new(64); private readonly List<Herbivore> _hBuf=new(64);
  private float _timer;
  void Awake(){
    if(Instance!=null && Instance!=this){ Destroy(gameObject); return; } Instance=this; DontDestroyOnLoad(gameObject);
    float cell=(ConfigService.Instance?.Sim?.cellSize)??2f; _producers=new(cell); _herbivores=new(cell);
  }
  void Update(){
    float interval=(ConfigService.Instance?.Sim?.indexRebuildInterval)??0.25f; _timer+=Time.unscaledDeltaTime; if(_timer<interval) return; _timer=0f; Rebuild();
  }
  public void Rebuild(){
    _producers.Clear(); _herbivores.Clear();
    foreach(var p in Producer.All) if(p!=null) _producers.Add(p.transform.position, p);
    foreach(var h in Herbivore.All) if(h!=null) _herbivores.Add(h.transform.position, h);
  }
  public int QueryProducers(Vector2 pos, float radius, List<Producer> outList){
    _producers.Query(pos,radius,_pBuf); outList.Clear(); float r2=radius*radius;
    for(int i=0;i<_pBuf.Count;i++){ var p=_pBuf[i]; if(p==null) continue; if(((Vector2)p.transform.position - pos).sqrMagnitude<=r2) outList.Add(p); } return outList.Count;
  }
  public int QueryHerbivores(Vector2 pos, float radius, List<Herbivore> outList){
    _herbivores.Query(pos,radius,_hBuf); outList.Clear(); float r2=radius*radius;
    for(int i=0;i<_hBuf.Count;i++){ var h=_hBuf[i]; if(h==null) continue; if(((Vector2)h.transform.position - pos).sqrMagnitude<=r2) outList.Add(h); } return outList.Count;
  }
}
