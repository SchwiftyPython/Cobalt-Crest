using Audio;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName="Ecosphere/Simulation Config", fileName="SimulationConfig")]
    public class SimulationConfig : ScriptableObject 
    {
        public Vector2 worldMin=new Vector2(-12,-7), worldMax=new Vector2(12,7);
        
        public bool usePooling=false, useEvolution=false, useShelters=false, useURP2D=false;
        
        [Header("Audio")]
        public bool enableAudio = true;
        public bool useFMOD = false;       
        [Range(0f, 1f)] public float audioVolume = 0.8f;
        [Range(1, 64)] public int audioVoiceLimit = 12;
        [Min(0)] public int chimeCooldownMs = 40;
        public ScaleQuantizer.Scale scale = ScaleQuantizer.Scale.Pentatonic;
        [Range(0, 8)] public int minOctave = 3;
        [Range(0, 8)] public int maxOctave = 6;
        
        [Range(30,240)] public int targetFps=60;
        
        [Header("Performance Tuning")]
        [Min(0.25f)] public float indexRebuildInterval = 0.25f;
        [Min(0.25f)] public float defaultSeekInterval = 0.5f;
        [Min(0.25f)] public float carnivoreSeekInterval = 0.35f;
        [Min(0.25f)] public float herbivoreWanderInterval = 1.0f;
        [Min(0.25f)] public float cellSize = 2.0f;
    
        [Header("Evolution")]
        [Range(0f, 3f)] public float mutationScale = 1.0f; // multiplies each agent’s mutationSigma

    }
}