using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Logs unassigned [Required] violations to the console while a scene is edited. Opening a
    /// scene cannot be aborted, so this gate only reports — it never blocks. It covers every
    /// load path (manual open, additive open, scenes already open at editor start or after a
    /// domain reload) and also re-scans on edits — adding/renaming/reparenting GameObjects,
    /// attaching/detaching components, or assigning the reference — so the console reflects
    /// the current state. Edits are debounced so a burst re-scans once. Because the console is
    /// append-only the re-scan re-logs (older entries linger). Each reported scene is recorded
    /// in <see cref="RequiredGuardReportLog"/> so the play-mode gate can abort Play without
    /// logging the same violations again.
    /// </summary>
    [InitializeOnLoad]
    internal static class RequiredSceneOpenGuard
    {
        // Coalesce a burst of object changes into a single re-scan this many seconds after
        // the last change, so dragging or multi-step edits don't re-log on every event.
        private const double DebounceSeconds = 0.3;

        private static bool _rescanPending;
        private static double _lastChangeTime;

        static RequiredSceneOpenGuard()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            ObjectChangeEvents.changesPublished += OnObjectsChanged;
            EditorApplication.update += DrainPendingRescan;
            // Scenes already open at editor start or after a domain reload fire no
            // sceneOpened event, so sweep them once the editor is idle.
            EditorApplication.delayCall += ReportOpenScenes;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Report(scene);

        // Any hierarchy/structure/property change (add, rename, reparent, component add/remove,
        // reference assignment) lands here; re-scan after the burst settles.
        private static void OnObjectsChanged(ref ObjectChangeEventStream stream)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            _rescanPending = true;
            _lastChangeTime = EditorApplication.timeSinceStartup;
        }

        private static void DrainPendingRescan()
        {
            if (!_rescanPending) return;
            if (EditorApplication.timeSinceStartup - _lastChangeTime < DebounceSeconds) return;

            _rescanPending = false;
            ReportOpenScenes();
        }

        private static void ReportOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) Report(scene);
            }
        }

        private static void Report(Scene scene)
        {
            // Only report at edit time; runtime scene loads run the game's own flow and
            // any violation there would already have blocked Play.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!scene.IsValid()) return;

            var errors = new List<RequiredFieldChecker.Error>();
            RequiredFieldChecker.CollectSceneErrors(scene, errors);

            if (errors.Count == 0)
            {
                // The scene is clean now; drop any stale record so the play-mode gate will
                // report it again if it is broken before the next Play.
                RequiredGuardReportLog.ClearReported(scene.path);
                return;
            }

            foreach (var e in errors) RequiredFieldChecker.LogViolation(e);
            RequiredGuardReportLog.MarkReported(scene.path);
        }
    }
}
