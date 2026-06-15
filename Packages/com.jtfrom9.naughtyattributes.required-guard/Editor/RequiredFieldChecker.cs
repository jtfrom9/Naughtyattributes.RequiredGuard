using System.Collections.Generic;
using NaughtyAttributes;
using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace NaughtyAttributes.RequiredGuard.Editor
{
    /// <summary>
    /// Headless detector for [Required] ObjectReference fields that are left unassigned.
    /// Shared by the play-mode and build gates. Contains no GUI code, so the core
    /// <see cref="CollectErrors"/> logic is unit-testable in EditMode.
    /// </summary>
    public static class RequiredFieldChecker
    {
        public readonly struct Error
        {
            public readonly UnityObject Context;
            public readonly string Message;

            public Error(UnityObject context, string message)
            {
                Context = context;
                Message = message;
            }
        }

        /// <summary>
        /// Walks the serialized representation of <paramref name="obj"/> and appends one
        /// <see cref="Error"/> for every [Required] ObjectReference whose value is null.
        /// </summary>
        public static void CollectErrors(UnityObject obj, List<Error> errors)
        {
            if (obj == null) return;

            using var so = new SerializedObject(obj);
            SerializedProperty it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                // Descend into children so [Required] inside nested [Serializable] types is reached.
                enter = true;
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

                var required = PropertyUtility.GetAttribute<RequiredAttribute>(it);
                if (required == null) continue;
                if (it.objectReferenceValue != null) continue;

                string msg = string.IsNullOrEmpty(required.Message)
                    ? $"{obj.name}.{it.propertyPath} is required but not assigned"
                    : $"{obj.name}.{it.propertyPath}: {required.Message}";
                errors.Add(new Error(obj, msg));
            }
        }

        /// <summary>
        /// Convenience wrapper that checks every MonoBehaviour in a loaded scene.
        /// Used by both gates so the traversal lives in one place.
        /// </summary>
        public static void CollectSceneErrors(Scene scene, List<Error> errors)
        {
            if (!scene.IsValid()) return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                // includeInactive: true -> disabled objects still ship in the build.
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue; // Missing/broken script reference.
                    CollectErrors(component, errors);
                }
            }
        }
    }
}
