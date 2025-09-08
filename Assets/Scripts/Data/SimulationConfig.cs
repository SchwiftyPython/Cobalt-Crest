using UnityEngine;
[CreateAssetMenu(menuName="Ecosphere/Simulation Config", fileName="SimulationConfig")]
public class SimulationConfig : ScriptableObject {
  public Vector2 worldMin=new Vector2(-12,-7), worldMax=new Vector2(12,7);
  public bool usePooling=false, useEvolution=false, useShelters=false, useFMOD=false, useURP2D=false;
  [Range(30,240)] public int targetFps=60;
}
