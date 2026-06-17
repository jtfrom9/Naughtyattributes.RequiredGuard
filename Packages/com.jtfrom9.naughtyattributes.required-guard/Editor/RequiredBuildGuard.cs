using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Fails the build when any enabled build scene contains an unassigned [Required] field.
    /// Throwing <see cref="BuildFailedException"/> aborts the build and returns a non-zero
    /// exit code, so the gate also works in batch-mode CI.
    /// </summary>
    internal sealed class RequiredBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Opening scenes mutates the editor's scene setup, so capture it first and
            // restore it in a finally block even when the gate aborts the build.
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            int errorCount = 0;
            try
            {
                foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
                {
                    if (!buildScene.enabled) continue;

                    var sceneErrors = new List<RequiredFieldChecker.Error>();
                    Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                    RequiredFieldChecker.CollectSceneErrors(scene, sceneErrors);

                    // Log while this scene is still open so the context is live at log
                    // time. The build aborts and the original SceneSetup is restored
                    // afterwards, destroying these objects — so prefix the scene path to
                    // keep each violation identifiable in the console (R3) even when
                    // same-named objects exist across multiple build scenes.
                    foreach (var e in sceneErrors)
                        RequiredFieldChecker.LogViolation(e, $"[{buildScene.path}] ");
                    errorCount += sceneErrors.Count;
                }
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
            }

            if (errorCount > 0)
            {
                throw new BuildFailedException($"{errorCount} required field(s) are unassigned");
            }
        }
    }
}
