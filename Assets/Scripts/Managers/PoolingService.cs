using System.Collections.Generic; using UnityEngine;
public class PoolingService : MonoBehaviour {
  public static PoolingService Instance { get; private set; }
  private readonly Stack<Producer> _prod = new(); private readonly Stack<Herbivore> _herb = new(); private readonly Stack<Carnivore> _carn = new();
  void Awake(){ if(Instance!=null && Instance!=this){ Destroy(gameObject); return; } Instance=this; DontDestroyOnLoad(gameObject); }
  bool UsePooling => (ConfigService.Instance?.Sim?.usePooling)??false;
  public Producer SpawnProducer(Vector2 pos){
    if(!UsePooling || _prod.Count==0){ var go=new GameObject("Producer"); var c=go.AddComponent<Producer>(); go.transform.position=pos; return c; }
    var pr=_prod.Pop(); pr.gameObject.SetActive(true); pr.transform.position=pos; return pr;
  }
  public Herbivore SpawnHerbivore(Vector2 pos){
    if(!UsePooling || _herb.Count==0){ var go=new GameObject("Herbivore"); var c=go.AddComponent<Herbivore>(); c.SetupOnSpawn(pos); return c; }
    var h=_herb.Pop(); h.gameObject.SetActive(true); h.SetupOnSpawn(pos); return h;
  }
  public Carnivore SpawnCarnivore(Vector2 pos){
    if(!UsePooling || _carn.Count==0){ var go=new GameObject("Carnivore"); var c=go.AddComponent<Carnivore>(); c.SetupOnSpawn(pos); return c; }
    var c2=_carn.Pop(); c2.gameObject.SetActive(true); c2.SetupOnSpawn(pos); return c2;
  }
  public void Despawn(Producer p){ if(!UsePooling){ if(p!=null) Object.Destroy(p.gameObject); return; } if(p==null) return; p.gameObject.SetActive(false); _prod.Push(p); }
  public void Despawn(Herbivore h){ if(!UsePooling){ if(h!=null) Object.Destroy(h.gameObject); return; } if(h==null) return; h.gameObject.SetActive(false); _herb.Push(h); }
  public void Despawn(Carnivore c){ if(!UsePooling){ if(c!=null) Object.Destroy(c.gameObject); return; } if(c==null) return; c.gameObject.SetActive(false); _carn.Push(c); }
}
