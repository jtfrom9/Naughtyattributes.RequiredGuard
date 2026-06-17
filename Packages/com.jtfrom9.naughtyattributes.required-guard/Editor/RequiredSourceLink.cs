using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Resolves a [Required] violation to an "Assets/.../File.cs:line" label that the gates
    /// embed in the console message, so a reader can see exactly where the offending field is
    /// declared. (The console's double-click jump is driven by the stack trace, which can't
    /// point at a data field, so the location is surfaced as text instead.)
    /// </summary>
    public static class RequiredSourceLink
    {
        /// <summary>
        /// Returns "Assets/.../File.cs:line" (the project-relative script path and 1-based
        /// line) for the offending field, or null when the owning script can't be located.
        /// Falls back to line 1 when the exact line can't be found.
        /// </summary>
        public static string DescribeLocation(UnityObject context, string propertyPath)
        {
            MonoScript script = GetScript(context);
            if (script == null) return null;

            string path = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(path)) return null;

            int line = 0;
            string fieldName = LeafName(propertyPath);
            try
            {
                if (!string.IsNullOrEmpty(fieldName) && File.Exists(path))
                    line = FindFieldLine(File.ReadAllLines(path), fieldName);
            }
            catch (IOException)
            {
                line = 0;
            }

            if (line <= 0) line = 1; // At least name the right script.
            return $"{path}:{line}";
        }

        /// <summary>
        /// Returns the 1-based line of the first declaration of <paramref name="fieldName"/>
        /// in <paramref name="lines"/>, or 0 when not found. Matches the whole identifier so a
        /// field named "_body" does not resolve to "_bodyPart". Pure, so it is unit-tested.
        /// </summary>
        public static int FindFieldLine(IReadOnlyList<string> lines, string fieldName)
        {
            if (lines == null || string.IsNullOrEmpty(fieldName)) return 0;

            var pattern = new Regex($@"\b{Regex.Escape(fieldName)}\b");
            for (int i = 0; i < lines.Count; i++)
            {
                if (pattern.IsMatch(lines[i])) return i + 1;
            }
            return 0;
        }

        private static MonoScript GetScript(UnityObject context)
        {
            switch (context)
            {
                case MonoBehaviour mb: return MonoScript.FromMonoBehaviour(mb);
                case ScriptableObject so: return MonoScript.FromScriptableObject(so);
                default: return null;
            }
        }

        // The serialized path of a nested field is "outer.inner.leaf"; the declaration we
        // want to locate is the leaf, so drop the parent segments.
        private static string LeafName(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath)) return null;
            int dot = propertyPath.LastIndexOf('.');
            return dot < 0 ? propertyPath : propertyPath.Substring(dot + 1);
        }
    }
}
