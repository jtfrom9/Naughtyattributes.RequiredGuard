# NaughtyAttributes Required Field Guard

Blocks entering Play mode and fails player builds when a `[Required]` (NaughtyAttributes)
ObjectReference field is left unassigned. Pairs well with `#nullable enable` + `= null!`.
Use `[RequiredGuardIgnore]` to opt a single field out of blocking.

**Full documentation:** https://github.com/jtfrom9/Naughtyattributes.RequiredGuard

## Install

Depends on NaughtyAttributes (`com.dbrizov.naughtyattributes`). Via OpenUPM:

```sh
openupm add com.jtfrom9.naughtyattributes.required-guard
```

…or a scoped registry / Git URL — see the repository README for details.

## License

[MIT](LICENSE) © jtfrom9 (Jun Tachikawa)
