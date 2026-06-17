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

            int errorCount = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var sceneErrors = new List<RequiredFieldChecker.Error>();
                RequiredFieldChecker.CollectSceneErrors(scene, sceneErrors);
                if (sceneErrors.Count == 0) continue;

                errorCount += sceneErrors.Count;

                // The scene-open gate already logged this scene's violations when it was
                // loaded; don't repeat them. Play is still aborted below regardless.
                if (RequiredGuardReportLog.IsReported(scene.path)) continue;

                foreach (var e in sceneErrors) RequiredFieldChecker.LogViolation(e);
            }

            if (errorCount == 0) return;

            EditorApplication.isPlaying = false; // Undo the play request.
        }
    }
}
