using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Cancels entering play mode when any currently open scene contains an unassigned
    /// [Required] field. Only the open scenes matter here: those are what actually run.
    /// </summary>
    [InitializeOnLoad]
    internal static class RequiredPlayModeGuard
    {
        static RequiredPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnChange;
        }

        private static void OnChange(PlayModeStateChange state)
        {
            // ExitingEditMode fires after Play is pressed but before the game starts,
            // which is the last point we can still abort cleanly.
            if (state != PlayModeStateChange.ExitingEditMode) return;

            var errors = new List<RequiredFieldChecker.Error>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) RequiredFieldChecker.CollectSceneErrors(scene, errors);
            }

            if (errors.Count == 0) return;

            foreach (var e in errors) Debug.LogError(e.Message, e.Context);
            EditorApplication.isPlaying = false; // Undo the play request.
        }
    }
}
