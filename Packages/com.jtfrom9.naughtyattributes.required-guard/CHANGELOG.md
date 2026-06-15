# Changelog

All notable changes to this package will be documented in this file.

## [0.1.0] - Unreleased

### Added
- `RequiredFieldChecker`: headless detector for unassigned `[Required]` ObjectReference fields.
- `RequiredPlayModeGuard`: cancels entering Play mode when an open scene has violations (`NAUGHTY_REQUIRED_GUARD`).
- `RequiredBuildGuard`: fails the build when an enabled build scene has violations (`NAUGHTY_REQUIRED_GUARD`).
- EditMode tests for the detection logic.
