using System;

namespace NaughtyAttributes.RequiredGuard
{
    /// <summary>
    /// Opts a single [Required] field out of the Required Field Guard's Play/Build
    /// enforcement. NaughtyAttributes still draws its inspector warning, but the guard
    /// will not cancel Play or fail the build for this field — use it for references that
    /// are intentionally assigned at runtime rather than in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredGuardIgnoreAttribute : Attribute
    {
    }
}
