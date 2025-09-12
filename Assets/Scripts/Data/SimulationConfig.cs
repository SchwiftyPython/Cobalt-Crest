using Audio;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName="Ecosphere/Simulation Config", fileName="SimulationConfig")]
    public class SimulationConfig : ScriptableObject 
    {
        public Vector2 worldMin=new Vector2(-12,-7), worldMax=new Vector2(12,7);
        
        public bool usePooling=false, useEvolution=false, useURP2D=false;
        
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
        
        [Header("Shelters")]
        public bool sheltersEnabled = true;
        [Min(0.5f)] public float shelterCellSize = 2.0f;
        [Min(0.05f)] public float shelterStepInterval = 0.5f;
        [Range(0f,1f)] public float shelterInitialFill = 0.25f;
        // CA tuning
        [Range(0f,1f)] public float shelterSpread = 0.18f;
        [Range(0f,1f)] public float shelterDecay  = 0.03f;
        [Range(0f,0.2f)] public float shelterNoise = 0.01f;
        // Gameplay mapping
        [Min(1f)] public float movementCostInShelter = 1.5f;   // speed divisor at density=1
        [Range(0f,1f)] public float movementAvoidStrength = 0.6f; // steering away from dense regions
        public float producerGrowthInShelter = 1.2f;           // <1 = penalty, >1 = buff
        public bool showShelterOverlay = false;                // press 'G' to toggle at runtime
        
        // --- Steering (2.6) ---
        [Header("Steering")]
        [Min(0.05f)] public float steerUpdateInterval = 0.2f;
        [Range(30f, 720f)] public float maxTurnDegPerSec = 300f;

        [Header("Weights")]
        [Range(0f, 4f)] public float goalWeight = 1.0f;
        [Range(0f, 4f)] public float separationWeight = 1.2f;
        [Range(0f, 4f)] public float predatorAvoidWeight = 2.0f;   // herbivore only
        [Range(0f, 3f)] public float shelterAvoidWeight = 0.6f;     // additive to existing movementAvoidStrength

        [Header("Perception")]
        [Min(0.2f)] public float separationRadius = 1.2f;           // meters
        [Min(0.2f)] public float predatorThreatRadius = 3.0f;

        [Header("Heading Sampler")]
        public bool headingSamplesEnabled = true;
        [Range(3, 11)] public int headingSampleCount = 7;           // odd number (includes forward)
        [Range(10f, 160f)] public float headingSampleFovDeg = 100f;
        [Min(0.2f)] public float headingSampleLookahead = 1.2f;
        [Range(0f, 3f)] public float headingSampleWeight = 1.0f;    // blend into steering

        [Header("Noise")]
        [Range(0f, 1f)] public float jitterStrength = 0.15f;

    }
}