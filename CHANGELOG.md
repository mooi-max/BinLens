# Changelog

All notable changes to BinLens are documented in this file.

## 0.3.0 - 2026-08-06

- Batch analysis now uses a single input box with automatic format detection: paste `sudo -l` rules or a SUID path list and the input is routed to the right parser automatically. The manual mode switch is gone.
- Batch results are grouped by importance: exact matches and official aliases first (green), version-family and no-SUID-usage matches next (yellow), not-listed and forbidden rules last (red).
- The batch detail panel gained context filter buttons (All / Sudo / SUID / Capabilities / Unprivileged); selecting an exact SUID match shows its SUID commands by default, while a no-SUID-usage match shows every related context so the relevant commands are never hidden.
- Batch analysis now shows a parsing summary in the status bar (sudo rules vs SUID paths) and supports `Ctrl+Enter` to run, with a placeholder hint in the input box.
- Fixed button text/theme contrast issues by making theme application refresh all dynamically styled buttons (including on startup with a saved dark theme).

## 0.2.0 - 2026-08-06

- Added SUID batch analysis: paste the absolute-path output of `find / -perm -u=s -type f 2>/dev/null` (or a similar SUID file list) and match every binary against the GTFOBins `suid` entries in one pass.
- Batch analysis now has an explicit mode switch between `sudo -l` output and SUID file lists, with per-mode descriptions, empty-state hints and detail views (SUID results render the official `suid` commands instead of the `sudo` ones).
- SUID results distinguish exact matches, official aliases, version-family matches ("confirm version"), binaries that exist in GTFOBins but have no SUID usage, and binaries not listed at all.
- Expanded the built-in self-test with SUID parsing coverage (exact, alias, family, no-SUID-usage, not-listed and noise-line cases).
- Documented both batch analysis modes, collection commands and result-state meanings in the README.

## 0.1.4 - 2026-07-31

- Fixed GitHub Actions restores for self-contained Windows release builds.

## 0.1.3 - 2026-07-31

- Improved dark-theme text-input caret contrast.
- Refined light-theme context-filter selection states for clear, non-black active buttons.
- Changed the Windows title bar to show the BinLens version.

## 0.1.2 - 2026-07-31

- Changed command-box hover cursor to a hand pointer to make click-to-copy discoverable.

## 0.1.1 - 2026-07-30

- Restored clearly separated command-detail sections for Sudo, SUID, limited SUID, Capabilities and unprivileged contexts.

## 0.1.0 - 2026-07-30

- First public release of the offline Windows application.
- Added local GTFOBins search, context filters, click-to-copy commands and `sudo -l` batch analysis.
- Added Chinese/English UI and light/dark themes.
- Introduced a Codex-inspired neutral visual system: white light theme, graphite dark theme and semantic result states.
- Added GitHub Actions release automation with executable SHA-256 checksums.
