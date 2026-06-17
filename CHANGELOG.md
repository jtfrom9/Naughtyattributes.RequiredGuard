# Changelog

All notable changes to this package will be documented in this file.

## [1.0.0] - 2026-06-17

### Added
- `RequiredFieldChecker`: headless detector for unassigned `[Required]` ObjectReference fields.
- `RequiredPlayModeGuard`: cancels entering Play mode when an open scene has violations.
- `RequiredBuildGuard`: fails the build when an enabled build scene has violations.
- EditMode tests for the detection logic.

### Notes
- Installing the package is the opt-in; there is no scripting define to enable. The gates are
  always active once installed.
