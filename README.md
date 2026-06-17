# NaughtyAttributes Required Field Guard

[![openupm](https://img.shields.io/npm/v/com.jtfrom9.naughtyattributes.required-guard?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.jtfrom9.naughtyattributes.required-guard/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)

Blocks **entering Play mode** and **fails player builds** when a
[`[Required]`](https://github.com/dbrizov/NaughtyAttributes#required) (NaughtyAttributes)
ObjectReference field is left unassigned — and surfaces the same violations in the **editor
console while you edit**: click an entry to select the offending GameObject, double-click to
jump to the field in code.

NaughtyAttributes itself is not modified: this extension reuses its `[Required]` attribute and
only changes the *behavior* (Play/Build enforcement). **Installing the package is the opt-in** —
once installed it always enforces; if you don't want enforcement, don't install it.

## Intended use: pair it with `#nullable enable`

The main purpose is to keep C# nullable reference types enabled while writing serialized
references as **non-nullable**.

Under `#nullable enable`, a reference field you intend to assign in the Inspector triggers a
compiler warning ("non-nullable field must contain a non-null value..."). Silencing it with
`= null!` tells the compiler "trust me, it's assigned" — but nothing actually guarantees that.
`[Required]` plus this guard provides that guarantee for the scene fields it covers
(see [Scan scope](#scan-scope)).

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

## Editor console reporting

While you edit, violations in the **open scenes** are logged to the console — not only at
Play/Build. The open scenes are re-scanned (debounced) when you change the hierarchy
(add / rename / reparent a GameObject, attach / detach a component) or assign the reference,
so the console keeps reflecting the current state.

Each entry reads `<message>: <Component>.<field> [<GameObject/hierarchy/path>]` — for example:

```
Required field is not assigned: Player._body [Level/Player]
```

`<message>` is the `[Required("…")]` text, or `Required field is not assigned` when none is
given; the bracket holds the offending GameObject's full hierarchy path. Each entry is both:

- **selectable** — clicking it selects the GameObject in the Hierarchy (and the Inspector), so
  you can see which object is at fault;
- **double-clickable** — jumping to the field's declaration (`File.cs:line`) via a synthetic
  stack frame (a field nested in a `[Serializable]` type declared in another file falls back to
  the component's script).

This reporting never blocks (opening or editing a scene can't be aborted) — Play and Build stay
the hard gates. The console is append-only, so after you fix a field the older entries remain
until you clear the console.

## Installation

Run the [OpenUPM CLI](https://openupm.com/docs/getting-started.html) from your Unity project root:

```sh
openupm add com.jtfrom9.naughtyattributes.required-guard
```

This configures the OpenUPM scoped registry and pulls the NaughtyAttributes dependency
(`com.dbrizov.naughtyattributes`) from OpenUPM automatically.

## Scan scope

| Target | Editor console | Play | Build |
|--------|----------------|------|-------|
| GameObjects/Components in open scenes | ✅ live, report-only | ✅ blocks Play | — |
| Scenes in `EditorBuildSettings.scenes` (enabled) | only while open | — | ✅ each opened and validated |
| Standalone Prefab / ScriptableObject assets | out of scope | out of scope | out of scope |

Within a scanned scene, the guard checks **single `[Required]` ObjectReference fields**,
including ones nested inside `[Serializable]` types — even when those types are elements of a
serialized array/List — on **active and inactive** GameObjects.

`[Required]` on an **array/List itself** (or on a value type) is not supported and is never
enforced: NaughtyAttributes only validates single reference fields — its inspector just shows a
"works only on reference types" warning — so individual null elements are not checked.

## License

[MIT](LICENSE) © jtfrom9 (Jun Tachikawa)
