using NaughtyAttributes.RequiredGuard.Editor;
using NUnit.Framework;

namespace NaughtyAttributes.RequiredGuard.Editor.Tests
{
    public class RequiredSourceLinkTest
    {
        // 1-based line numbers are what the Unity console expects in "(at file:line)".
        private static readonly string[] Source =
        {
            "using UnityEngine;",                                  // 1
            "public class Player : MonoBehaviour",                // 2
            "{",                                                  // 3
            "    [SerializeField, Required] private Rigidbody _body = null!;", // 4
            "    [SerializeField] private GameObject _bodyPart;",  // 5
            "}",                                                  // 6
        };

        [Test]
        public void FindsDeclarationLine_OneBased()
        {
            Assert.AreEqual(4, RequiredSourceLink.FindFieldLine(Source, "_body"));
        }

        [Test]
        public void MatchesWholeIdentifier_NotSubstrings()
        {
            // "_body" must not match "_bodyPart" on line 5; the real decl is line 4.
            Assert.AreEqual(4, RequiredSourceLink.FindFieldLine(Source, "_body"));
            // And the longer field resolves to its own line.
            Assert.AreEqual(5, RequiredSourceLink.FindFieldLine(Source, "_bodyPart"));
        }

        [Test]
        public void ReturnsZero_WhenFieldNotFound()
        {
            Assert.AreEqual(0, RequiredSourceLink.FindFieldLine(Source, "_missing"));
        }

        [Test]
        public void ReturnsZero_ForEmptyOrNullInput()
        {
            Assert.AreEqual(0, RequiredSourceLink.FindFieldLine(Source, ""));
            Assert.AreEqual(0, RequiredSourceLink.FindFieldLine(null, "_body"));
        }

        [Test]
        public void ReturnsFirstMatch_WhenNameAppearsMultipleTimes()
        {
            string[] src =
            {
                "private GameObject target;", // 1: declaration
                "target = null;",             // 2: usage
            };
            Assert.AreEqual(1, RequiredSourceLink.FindFieldLine(src, "target"));
        }
    }
}
