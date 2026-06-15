# NaughtyAttributes Required Field Guard

Blocks **entering Play mode** and **fails player builds** when a
[`[Required]`](https://github.com/dbrizov/NaughtyAttributes#required) (NaughtyAttributes)
ObjectReference field is left unassigned.

NaughtyAttributes itself is not modified: this extension reuses its `[Required]` attribute and
only changes the *behavior* (Play/Build enforcement). **Installing the package is the opt-in** —
once installed it always enforces; if you don't want enforcement, don't install it.

## Intended use: pair it with `#nullable enable`

The main purpose is to keep C# nullable reference types enabled while writing serialized
references as **non-nullable**.

Under `#nullable enable`, a reference field you intend to assign in the Inspector triggers a
compiler warning ("non-nullable field must contain a non-null value..."). Silencing it with
`= null!` tells the compiler "trust me, it's assigned" — but nothing actually guarantees that.
`[Required]` plus this guard provides the guarantee.

```csharp
#nullable enable
using NaughtyAttributes;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Written as non-nullable. `null!` silences the compiler, and if it is left
    // unassigned the guard stops Play/Build, so the `null!` promise actually holds.
    [SerializeField, Required] private Rigidbody _body = null!;

    private void FixedUpdate()
    {
        _body.AddForce(Vector3.up); // no null check needed
    }
}
```

This turns "wrote `null!` but forgot to assign it" from a runtime `NullReferenceException`
into a Play/Build-time failure.

## Opting a field out: `[RequiredGuardIgnore]`

For a field that should still show the `[Required]` Inspector warning but must **not** block
Play/Build (e.g. assigned at runtime), add the opt-out marker:

```csharp
#nullable enable
using NaughtyAttributes;
using NaughtyAttributes.RequiredGuard;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // Still shows the red Inspector warning, but the guard won't block (assigned at runtime).
    [SerializeField, Required, RequiredGuardIgnore] private GameObject _spawned = null!;
}
```

## How it works

| Part | Assembly | Role |
|------|----------|------|
| `RequiredGuardIgnoreAttribute` | Runtime | Opt-out marker excluding a field from Play/Build blocking |
| `RequiredFieldChecker` | Editor | Headless detection logic (unit-tested) |
| `RequiredPlayModeGuard` | Editor | Validates open scenes and cancels Play on a violation |
| `RequiredBuildGuard` | Editor | Validates enabled build scenes and fails the build on a violation |

## Installation

This package depends on NaughtyAttributes (`com.dbrizov.naughtyattributes`), which is not on
Unity's built-in registry. Provide a resolvable path for it in the consuming project (either
option below).

### Option A: Git URL

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.jtfrom9.naughtyattributes.required-guard": "https://github.com/jtfrom9/Naughtyattributes.RequiredGuard.git?path=/Packages/com.jtfrom9.naughtyattributes.required-guard#v0.1.0",
    "com.dbrizov.naughtyattributes": "https://github.com/dbrizov/NaughtyAttributes.git#v2.1.4"
  }
}
```

> Unity's `?path=` query installs a package from a subfolder of a git repository.

### Option B: OpenUPM (scoped registry)

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jtfrom9",
        "com.dbrizov.naughtyattributes"
      ]
    }
  ],
  "dependencies": {
    "com.jtfrom9.naughtyattributes.required-guard": "0.1.0",
    "com.dbrizov.naughtyattributes": "2.1.4"
  }
}
```

Or via the OpenUPM CLI:

```sh
openupm add com.jtfrom9.naughtyattributes.required-guard
```

## Scan scope (initial version)

| Target | Play | Build |
|--------|------|-------|
| GameObjects/Components in open scenes | ✅ | — |
| Scenes in `EditorBuildSettings.scenes` (enabled) | — | ✅ each opened and validated |
| Standalone Prefab / ScriptableObject assets | out of scope | out of scope |

## Limitations (v0.1)

- Detects `[Required]` on **ObjectReference fields** (including those inside nested
  `[Serializable]` types). **Array / List elements** are out of scope in this version.
- `[Required]` on non-ObjectReference types (string/int, etc.) is ignored.
- Standalone Prefab / ScriptableObject assets are not scanned (see table above).
- Fields marked `[RequiredGuardIgnore]` are excluded from Play/Build blocking (the Inspector
  warning is still drawn by NaughtyAttributes).

## Development & testing

This repository is itself a Unity project; the package is embedded at
`Packages/com.jtfrom9.naughtyattributes.required-guard/`. Run the EditMode tests from the Unity
Test Runner, or via CLI:

```sh
Unity.exe -batchmode -runTests -projectPath <repo> -testPlatform EditMode -testResults results.xml
```

(Do not combine `-runTests` with `-quit` — the test runner quits on its own once tests finish.)

## License

[MIT](LICENSE) © jtfrom9 (Jun Tachikawa)
