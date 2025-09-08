using System.Collections.Generic; using UnityEngine;
[RequireComponent(typeof(SpriteRenderer))] public class Carnivore:MonoBehaviour{
  public static readonly List<Carnivore> All=new();
  public float speed=2.2f, metabolism=0.6f, energy=5f, attackRange=0.7f, attackDamage=100f, reproduceThreshold=10f, childCost=4.5f, sightRadius=6f;
  Vector2 _dir; float _retargetTimer; SpriteRenderer _sr;
  void OnEnable(){ All.Add(this); EcosystemManager.Instance?.Increment("Carnivores"); }
  void OnDisable(){ All.Remove(this); EcosystemManager.Instance?.Decrement("Carnivores"); }
  void Awake(){
    _sr=GetComponent<SpriteRenderer>();
    var color=ConfigService.Instance? ConfigService.Instance.GetSpeciesColor(SpeciesId.Carnivore, Color.red): Color.red;
    _sr.sprite=SpriteFactory.CreateDiscSprite(color,16); _dir=Random.insideUnitCircle.normalized;
    var def=ConfigService.Instance? ConfigService.Instance.GetSpecies(SpeciesId.Carnivore): null;
    if(def!=null){ speed=def.speed; metabolism=def.metabolism; attackRange=def.attackRange; attackDamage=def.attackDamage; reproduceThreshold=def.reproduceThreshold; childCost=def.childCost; sightRadius=def.sightRadius; }
  }
  void Update(){
    float dt=Time.deltaTime*EcosystemManager.SimulationSpeed; _retargetTimer-=dt;
    if(_retargetTimer<=0f){ _retargetTimer=0.35f+Random.value*0.35f; var prey=FindNearestHerbivore();
      if(prey!=null) _dir=((Vector2)prey.transform.position-(Vector2)transform.position).normalized; else _dir=Vector2.Lerp(_dir, Random.insideUnitCircle.normalized, 0.5f); }
    transform.position+=(Vector3)(_dir*speed*dt); energy-=metabolism*dt;
    var h=FindNearestHerbivore(); if(h!=null && Vector2.Distance(transform.position,h.transform.position)<attackRange){ Destroy(h.gameObject); energy+=3.5f; }
    if(energy>=reproduceThreshold){ energy-=childCost; Spawner.SpawnCarnivore((Vector2)transform.position+Random.insideUnitCircle*0.5f); }
    if(energy<=0f) Destroy(gameObject);
  }
  Herbivore FindNearestHerbivore(){ Herbivore best=null; float bestD=sightRadius; foreach(var h in Herbivore.All){ float d=Vector2.Distance(transform.position,h.transform.position); if(d<bestD){bestD=d; best=h;} } return best; }
}
