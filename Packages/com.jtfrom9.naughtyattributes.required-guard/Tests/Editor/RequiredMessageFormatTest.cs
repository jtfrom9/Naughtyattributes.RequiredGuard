using NaughtyAttributes.RequiredGuard.Editor;
using NUnit.Framework;

namespace NaughtyAttributes.RequiredGuard.Editor.Tests
{
    public class RequiredMessageFormatTest
    {
        [Test]
        public void CustomMessage_HeadThenMemberThenGameObjectPath()
        {
            string msg = RequiredFieldChecker.BuildMessage(
                "A/B/C", "Test", "_requiredObject", "This field is required");

            Assert.AreEqual("This field is required: Test._requiredObject [A/B/C]", msg);
        }

        [Test]
        public void NoCustomMessage_UsesDefaultHead()
        {
            string msg = RequiredFieldChecker.BuildMessage(
                "A/B/C", "Test", "_requiredObject", null);

            Assert.AreEqual(
                $"{RequiredFieldChecker.DefaultMessage}: Test._requiredObject [A/B/C]", msg);
        }

        [Test]
        public void NoGameObjectPath_OmitsBracket()
        {
            string msg = RequiredFieldChecker.BuildMessage(
                null, "Config", "reference", "please assign");

            Assert.AreEqual("please assign: Config.reference", msg);
        }

        [Test]
        public void EmptyGameObjectPath_IsTreatedAsAbsent()
        {
            string msg = RequiredFieldChecker.BuildMessage(
                "", "Test", "_requiredObject", "");

            Assert.AreEqual($"{RequiredFieldChecker.DefaultMessage}: Test._requiredObject", msg);
        }
    }
}
