using System.Collections.Generic; using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))] public class Herbivore:MonoBehaviour{
  public static readonly List<Herbivore> All=new();
  public float speed=1.8f, metabolism=0.4f, energy=4f, eatRate=1.2f, reproduceThreshold=8f, childCost=3.5f, sightRadius=4f;
  Vector2 _dir; float _retargetTimer,_wanderTimer; SpriteRenderer _sr;
  void OnEnable(){ All.Add(this); EcosystemManager.Instance?.Increment("Herbivores"); }
  void OnDisable(){ All.Remove(this); EcosystemManager.Instance?.Decrement("Herbivores"); }
  void Awake(){
    _sr=GetComponent<SpriteRenderer>();
    var color=ConfigService.Instance? ConfigService.Instance.GetSpeciesColor(SpeciesId.Herbivore, Color.cyan): Color.cyan;
    _sr.sprite=SpriteFactory.CreateDiscSprite(color,14); _dir=Random.insideUnitCircle.normalized;
    var def=ConfigService.Instance? ConfigService.Instance.GetSpecies(SpeciesId.Herbivore): null;
    if(def!=null){ speed=def.speed; metabolism=def.metabolism; eatRate=def.eatRate; reproduceThreshold=def.reproduceThreshold; childCost=def.childCost; sightRadius=def.sightRadius; }
  }
  void Update(){
    float dt=Time.deltaTime*EcosystemManager.SimulationSpeed; _retargetTimer-=dt; _wanderTimer-=dt;
    if(_retargetTimer<=0f){ _retargetTimer=0.5f+Random.value*0.5f; var t=FindNearestProducer();
      if(t!=null) _dir=((Vector2)t.transform.position-(Vector2)transform.position).normalized;
      else if(_wanderTimer<=0f){ _dir=Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f); _wanderTimer=1f+Random.value; } }
    transform.position+=(Vector3)(_dir*speed*dt); energy-=metabolism*dt;
    var p=FindNearestProducer(); if(p!=null && Vector2.Distance(transform.position,p.transform.position)<0.6f){ energy+=p.Consume(eatRate*dt); }
    if(energy>=reproduceThreshold){ energy-=childCost; Spawner.SpawnHerbivore((Vector2)transform.position+Random.insideUnitCircle*0.5f); }
    if(energy<=0f) Destroy(gameObject);
  }
  Producer FindNearestProducer(){ Producer best=null; float bestD=sightRadius; foreach(var p in Producer.All){ float d=Vector2.Distance(transform.position,p.transform.position); if(d<bestD){bestD=d; best=p;} } return best; }
}
