using System;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Exception whose <see cref="StackTrace"/> is hand-built so that logging it via
    /// Debug.LogException places the offending field's declaration in the console callstack.
    /// Unity drives the console's double-click jump from the stack trace (not the message), so
    /// this is what makes the entry open the user's code instead of this package.
    /// </summary>
    internal sealed class RequiredFieldException : Exception
    {
        private readonly string _stackTrace;

        public RequiredFieldException(string message, string stackTrace) : base(message)
        {
            _stackTrace = stackTrace;
        }

        public override string StackTrace => _stackTrace;
    }
}
