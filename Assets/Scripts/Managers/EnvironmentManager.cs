using UnityEngine;
public class EnvironmentManager : MonoBehaviour {
  private float _t; public float CurrentTemp{get;private set;}=0.5f; public float CurrentRain{get;private set;}=0.5f;
  void Update(){
    float dt = Time.deltaTime * Mathf.Max(0.0001f, EcosystemManager.SimulationSpeed); _t += dt;
    var env = ConfigService.Instance?.Env; float yearLen = env!=null? Mathf.Max(0.01f, env.yearLengthSeconds):120f; float season = (_t % yearLen)/yearLen;
    var temp = env!=null? env.temperatureOverYear : AnimationCurve.Linear(0,0.5f,1,0.5f);
    var rain = env!=null? env.rainfallOverYear : AnimationCurve.Linear(0,0.5f,1,0.5f);
    CurrentTemp = Mathf.Clamp01(temp.Evaluate(season)); CurrentRain = Mathf.Clamp01(rain.Evaluate(season));
  }
}
