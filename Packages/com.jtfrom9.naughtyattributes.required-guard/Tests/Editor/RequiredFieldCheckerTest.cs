using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using NaughtyAttributes.RequiredGuard.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace NaughtyAttributes.RequiredGuard.Editor.Tests
{
    public class RequiredFieldCheckerTest
    {
        // --- Fixtures -------------------------------------------------------

        // A trivial UnityEngine.Object used as the assignable reference target.
        // Public so it can appear in the public [Serializable] nested fixtures below.
        public class Dummy : ScriptableObject { }

        private class WithRequired : ScriptableObject
        {
            [Required] public Dummy reference;
        }

        private class TwoRequired : ScriptableObject
        {
            [Required] public Dummy first;
            [Required] public Dummy second;
        }

        private class WithoutRequired : ScriptableObject
        {
            public Dummy reference; // No [Required] -> never reported.
        }

        private class RequiredOnValueTypes : ScriptableObject
        {
            // [Required] only means anything for ObjectReference fields. On value
            // types it must be ignored, and traversal must not throw.
            [Required] public string label;
            [Required] public int count;
        }

        private class WithCustomMessage : ScriptableObject
        {
            [Required("please assign me")] public Dummy reference;
        }

        [Serializable]
        public class Inner
        {
            [Required] public Dummy nestedReference;
        }

        private class WithNested : ScriptableObject
        {
            public Inner inner = new Inner();
        }

        [Serializable]
        public class Deep
        {
            [Required] public Dummy deepReference;
        }

        [Serializable]
        public class Mid
        {
            public Deep deep = new Deep();
        }

        private class WithDeepNested : ScriptableObject
        {
            public Mid mid = new Mid();
        }

        private class SceneComponent : MonoBehaviour
        {
            [Required] public Dummy reference;
        }

        // --- Helpers --------------------------------------------------------

        private readonly List<UnityObject> _spawned = new List<UnityObject>();

        // Stable instance name for the fixtures (message wording is covered separately).
        private const string FixtureName = "Fix";

        private T New<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            obj.name = FixtureName;
            _spawned.Add(obj);
            return obj;
        }

        private static List<RequiredFieldChecker.Error> Collect(UnityObject obj)
        {
            var errors = new List<RequiredFieldChecker.Error>();
            RequiredFieldChecker.CollectErrors(obj, errors);
            return errors;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _spawned)
            {
                if (o != null) UnityObject.DestroyImmediate(o);
            }
            _spawned.Clear();
        }

        // --- Detection: single ObjectReference ------------------------------

        [Test]
        public void UnassignedRequired_ReportsExactlyOneErrorForThatField()
        {
            var obj = New<WithRequired>();

            var errors = Collect(obj);

            Assert.AreEqual(1, errors.Count);
            Assert.AreSame(obj, errors[0].Context);
            Assert.AreEqual("reference", errors[0].PropertyPath);
        }

        [Test]
        public void AssignedRequired_ReportsNoError()
        {
            var obj = New<WithRequired>();
            obj.reference = New<Dummy>();

            Assert.AreEqual(0, Collect(obj).Count);
        }

        [Test]
        public void FieldWithoutRequired_IsIgnored()
        {
            Assert.AreEqual(0, Collect(New<WithoutRequired>()).Count);
        }

        [Test]
        public void RequiredOnValueTypeFields_AreIgnoredAndDoNotThrow()
        {
            // Only ObjectReference fields are subject to the guard; a [Required]
            // string/int must neither be reported nor break traversal.
            Assert.AreEqual(0, Collect(New<RequiredOnValueTypes>()).Count);
        }

        [Test]
        public void CustomMessage_IsCapturedOnTheError()
        {
            var obj = New<WithCustomMessage>();

            var errors = Collect(obj);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual("reference", errors[0].PropertyPath);
            Assert.AreEqual("please assign me", errors[0].CustomMessage);
        }

        // --- Detection: multiple violations ---------------------------------

        [Test]
        public void MultipleUnassignedRequired_ReportsEveryField()
        {
            var obj = New<TwoRequired>();

            var paths = Collect(obj).Select(e => e.PropertyPath).ToList();

            Assert.AreEqual(2, paths.Count);
            CollectionAssert.AreEquivalent(new[] { "first", "second" }, paths);
        }

        // --- Detection: nested [Serializable] -------------------------------

        [Test]
        public void NestedUnassignedRequired_IsDetectedWithNestedPath()
        {
            var obj = New<WithNested>();

            var errors = Collect(obj);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual("inner.nestedReference", errors[0].PropertyPath);
        }

        [Test]
        public void NestedAssignedRequired_ReportsNoError()
        {
            var obj = New<WithNested>();
            obj.inner.nestedReference = New<Dummy>();

            Assert.AreEqual(0, Collect(obj).Count);
        }

        [Test]
        public void DeeplyNestedUnassignedRequired_IsDetected()
        {
            var obj = New<WithDeepNested>();

            var errors = Collect(obj);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual("mid.deep.deepReference", errors[0].PropertyPath);
        }

        // --- Edge cases -----------------------------------------------------

        [Test]
        public void NullTarget_IsSafe()
        {
            Assert.AreEqual(0, Collect(null).Count);
        }

        // --- Scene traversal seam (shared by Play/Build gates) --------------

        [Test]
        public void CollectSceneErrors_FindsComponentViolationWithLiveContext()
        {
            Scene scene = SceneManager.GetActiveScene();
            var go = new GameObject("Root");
            try
            {
                SceneComponent component = go.AddComponent<SceneComponent>();

                var errors = new List<RequiredFieldChecker.Error>();
                RequiredFieldChecker.CollectSceneErrors(scene, errors);

                List<RequiredFieldChecker.Error> ours =
                    errors.Where(e => ReferenceEquals(e.Context, component)).ToList();
                Assert.AreEqual(1, ours.Count);
                Assert.AreEqual("reference", ours[0].PropertyPath);
            }
            finally
            {
                UnityObject.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectSceneErrors_IncludesInactiveObjects()
        {
            Scene scene = SceneManager.GetActiveScene();
            var go = new GameObject("Root");
            try
            {
                SceneComponent component = go.AddComponent<SceneComponent>();
                go.SetActive(false); // Inactive objects still ship in the build.

                var errors = new List<RequiredFieldChecker.Error>();
                RequiredFieldChecker.CollectSceneErrors(scene, errors);

                Assert.IsTrue(errors.Any(e => ReferenceEquals(e.Context, component)));
            }
            finally
            {
                UnityObject.DestroyImmediate(go);
            }
        }
    }
}
