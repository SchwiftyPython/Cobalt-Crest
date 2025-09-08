using UnityEngine;
[CreateAssetMenu(menuName="Ecosphere/Environment Definition", fileName="EnvironmentDefinition")]
public class EnvironmentDefinition : ScriptableObject {
  public float yearLengthSeconds=120f;
  public AnimationCurve temperatureOverYear=AnimationCurve.EaseInOut(0,0.5f,1,1.0f);
  public AnimationCurve rainfallOverYear=AnimationCurve.EaseInOut(0,0.6f,1,0.4f);
  public float perlinScale=0.1f;
}
