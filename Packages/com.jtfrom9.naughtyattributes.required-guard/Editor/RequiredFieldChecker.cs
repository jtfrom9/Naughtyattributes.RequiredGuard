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
        // Default head used when [Required] carries no explicit message.
        public const string DefaultMessage = "Required field is not assigned";

        // Synthetic callstack frame label shown above the "(at file:line)" the console jumps to.
        private const string StackFrameLabel = "NaughtyAttributes.RequiredGuard:Required ()";

        public readonly struct Error
        {
            public readonly UnityObject Context;

            // The component type name, serialized field path, and optional [Required] custom
            // message: the pieces the console message is composed from. Kept apart so the
            // gates can re-compose it with the GameObject path and "Assets/.../File.cs:line"
            // location resolved from the live object (unavailable to runtime-only fixtures).
            public readonly string ComponentName;
            public readonly string PropertyPath;
            public readonly string CustomMessage;

            public Error(UnityObject context, string componentName, string propertyPath, string customMessage)
            {
                Context = context;
                ComponentName = componentName;
                PropertyPath = propertyPath;
                CustomMessage = customMessage;
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

                errors.Add(new Error(obj, obj.GetType().Name, it.propertyPath, required.Message));
            }
        }

        /// <summary>
        /// Logs one violation. The message carries the readable, fully-qualified description;
        /// when the field's source can be resolved the log is emitted as an exception whose
        /// synthetic stack trace points at the field declaration, so a console double-click
        /// jumps straight there. <paramref name="scenePrefix"/> prefixes the build gate's
        /// "[scene] " tag.
        /// </summary>
        public static void LogViolation(in Error error, string scenePrefix = null)
        {
            string gameObjectPath = GameObjectPathOf(error.Context);
            string body = BuildMessage(gameObjectPath, error.ComponentName, error.PropertyPath, error.CustomMessage);
            string message = string.IsNullOrEmpty(scenePrefix) ? body : scenePrefix + body;

            // The source location is surfaced through the synthetic stack frame (so the entry
            // is double-clickable), not repeated in the message body.
            string location = RequiredSourceLink.DescribeLocation(error.Context, error.PropertyPath);
            if (string.IsNullOrEmpty(location))
            {
                // No resolvable source: a plain error still reports it, just without the jump.
                Debug.LogError(message, error.Context);
                return;
            }

            string stackTrace = $"{StackFrameLabel} (at {location})\n";
            Debug.LogException(new RequiredFieldException(message, stackTrace), error.Context);
        }

        /// <summary>
        /// Composes the assertion-style message
        /// "{message}: {componentName}.{propertyPath} [{gameObjectPath}]", omitting the
        /// bracketed GameObject path when it is unknown. Pure, so it is unit-tested.
        /// </summary>
        public static string BuildMessage(
            string gameObjectPath, string componentName, string propertyPath, string customMessage)
        {
            string head = string.IsNullOrEmpty(customMessage) ? DefaultMessage : customMessage;
            string suffix = string.IsNullOrEmpty(gameObjectPath) ? "" : $" [{gameObjectPath}]";
            return $"{head}: {componentName}.{propertyPath}{suffix}";
        }

        // Full hierarchy path of the object's GameObject ("A/B/C"), or null when the context
        // is not a scene Component (e.g. a ScriptableObject).
        private static string GameObjectPathOf(UnityObject context)
        {
            if (!(context is Component component) || component == null) return null;

            var names = new List<string>();
            for (Transform t = component.transform; t != null; t = t.parent)
                names.Add(t.name);
            names.Reverse();
            return string.Join("/", names);
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
