# NaughtyAttributes Required Field Guard

[![openupm](https://img.shields.io/npm/v/com.jtfrom9.naughtyattributes.required-guard?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.jtfrom9.naughtyattributes.required-guard/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)

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

## How it works

| Part | Assembly | Role |
|------|----------|------|
| `RequiredFieldChecker` | Editor | Headless detection logic (unit-tested) |
| `RequiredPlayModeGuard` | Editor | Validates open scenes and cancels Play on a violation |
| `RequiredBuildGuard` | Editor | Validates enabled build scenes and fails the build on a violation |

## Installation

This package depends on NaughtyAttributes (`com.dbrizov.naughtyattributes`), which is not on
Unity's built-in registry. Provide a resolvable path for it in the consuming project (either
option below).

### Option A: OpenUPM (scoped registry)

The recommended way. Via the [OpenUPM CLI](https://openupm.com/docs/getting-started.html):

```sh
openupm add com.jtfrom9.naughtyattributes.required-guard
```

This also resolves the NaughtyAttributes dependency from OpenUPM automatically. Or configure the
scoped registry by hand in `Packages/manifest.json`:

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

### Option B: Git URL

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
