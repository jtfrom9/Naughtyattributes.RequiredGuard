using System.Collections.Generic;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Per-session record of which scenes have already had their unassigned [Required]
    /// violations reported by the scene-open gate. The play-mode gate consults this so it
    /// can still abort Play on a violation without logging the same errors a second time.
    /// Keyed by scene path. The state is intentionally static and is wiped on every domain
    /// reload, which is also when the scene-open gate re-sweeps the open scenes.
    /// Exposed publicly only so it can be unit-tested across the assembly boundary.
    /// </summary>
    public static class RequiredGuardReportLog
    {
        private static readonly HashSet<string> _reported = new HashSet<string>();

        public static bool IsReported(string scenePath) => _reported.Contains(scenePath);

        public static void MarkReported(string scenePath) => _reported.Add(scenePath);

        public static void ClearReported(string scenePath) => _reported.Remove(scenePath);

        /// <summary>Test seam: drop all recorded scenes.</summary>
        public static void Reset() => _reported.Clear();
    }
}
