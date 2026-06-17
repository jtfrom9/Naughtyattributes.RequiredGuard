using NaughtyAttributes.RequiredGuard.Editor;
using NUnit.Framework;

namespace NaughtyAttributes.RequiredGuard.Editor.Tests
{
    public class RequiredGuardReportLogTest
    {
        private const string SceneA = "Assets/A.unity";
        private const string SceneB = "Assets/B.unity";

        [TearDown]
        public void TearDown()
        {
            // The log is static and survives between tests; wipe it each time.
            RequiredGuardReportLog.Reset();
        }

        [Test]
        public void UnmarkedScene_IsNotReported()
        {
            Assert.IsFalse(RequiredGuardReportLog.IsReported(SceneA));
        }

        [Test]
        public void MarkReported_MakesSceneReported()
        {
            RequiredGuardReportLog.MarkReported(SceneA);

            Assert.IsTrue(RequiredGuardReportLog.IsReported(SceneA));
        }

        [Test]
        public void ClearReported_MakesSceneUnreportedAgain()
        {
            RequiredGuardReportLog.MarkReported(SceneA);
            RequiredGuardReportLog.ClearReported(SceneA);

            Assert.IsFalse(RequiredGuardReportLog.IsReported(SceneA));
        }

        [Test]
        public void Reporting_IsTrackedPerScene()
        {
            RequiredGuardReportLog.MarkReported(SceneA);

            Assert.IsTrue(RequiredGuardReportLog.IsReported(SceneA));
            Assert.IsFalse(RequiredGuardReportLog.IsReported(SceneB));
        }

        [Test]
        public void MarkReported_IsIdempotent()
        {
            RequiredGuardReportLog.MarkReported(SceneA);
            RequiredGuardReportLog.MarkReported(SceneA);
            RequiredGuardReportLog.ClearReported(SceneA);

            // A single clear removes it; marking twice must not require two clears.
            Assert.IsFalse(RequiredGuardReportLog.IsReported(SceneA));
        }
    }
}
