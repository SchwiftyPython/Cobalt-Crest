#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public static class EcospherePhase1Setup
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string SessionKey = "EcospherePhase1SetupDone";

    [MenuItem("Ecosphere/Setup Phase 1")]
    public static void SetupMenu()
    {
        DoSetup();
    }

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        // Only run once per editor session, and defer until editor is fully ready
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        EditorApplication.delayCall += TryDeferredSetup;
    }

    private static void TryDeferredSetup()
    {
        // Don’t run during play/compile/import/updates
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            EditorApplication.isPlaying)
        {
            // Try again a bit later
            EditorApplication.delayCall += TryDeferredSetup;
            return;
        }

        // At this point the editor is safe to touch scenes
        if (!SessionState.GetBool(SessionKey, false))
        {
            SessionState.SetBool(SessionKey, true);
            DoSetup();
        }
    }

    private static void DoSetup()
    {
        // Ensure Scenes folder exists
        var dir = System.IO.Path.GetDirectoryName(ScenePath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        // Create a new empty scene only if MainScene doesn’t exist yet
        if (!System.IO.File.Exists(ScenePath))
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, ScenePath);
        }

        // Add to Build Settings if missing
        var scenes = EditorBuildSettings.scenes;
        var exists = false;
        foreach (var s in scenes) if (s.path == ScenePath) { exists = true; break; }
        if (!exists)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        // Open it (safe now)
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[Ecosphere] Phase 1 setup complete. Opened MainScene.");
    }
}
#endif
