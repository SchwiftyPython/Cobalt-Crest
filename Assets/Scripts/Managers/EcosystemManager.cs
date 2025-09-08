using System.Collections.Generic;
using Managers;
using UnityEngine;
public class EcosystemManager : MonoBehaviour {
  public static EcosystemManager Instance { get; private set; }
  public static float SimulationSpeed { get=>_simSpeed; set=>_simSpeed=Mathf.Max(0f,value);} private static float _simSpeed=1f;
  private readonly Dictionary<string,int> _counts=new(){ {"Producers",0},{"Herbivores",0},{"Carnivores",0} };
  void Awake(){ if(Instance!=null&&Instance!=this){Destroy(gameObject);return;} Instance=this; DontDestroyOnLoad(gameObject); Application.targetFrameRate=(ConfigService.Instance?.Sim?.targetFps)??60; }
  public void Increment(string k){ if(!_counts.ContainsKey(k))
    {
      _counts[k]=0;
    }

    _counts[k]++; }
  public void Decrement(string k){ if(!_counts.ContainsKey(k))
    {
      _counts[k]=0;
    }

    _counts[k]=Mathf.Max(0,_counts[k]-1); }
  public int GetCount(string k)=>_counts.TryGetValue(k, out var v)? v:0;
}
