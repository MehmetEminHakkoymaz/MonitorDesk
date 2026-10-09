# Changelog

## 0.8.0 — Eye comfort and DDC recovery (unreleased)

- Add an adjustable click-through warm overlay for all connected screens, removed on toggle-off or application close.
- Keep the filter separate from Windows Night light and all hardware brightness/contrast settings; describe overlay limitations in both READMEs.
- Increase read retry backoff and add bounded transient write retries with a post-write settling interval.
- Preserve native error codes, skip retries for unsupported writes and retain explicit stale-value handling.
- Pass 121 automated/read-only checks including simulated write recovery and failure boundaries. The user approved this version for commit and push; OMEN 25i hardware recovery and detailed warm-filter interaction tests have not been confirmed separately.

## Unreleased — Windows installer

- Add an Inno Setup recipe and PowerShell/CMD build entry points for a single Windows x64 setup EXE.
- Bundle the .NET Windows Desktop runtime for offline installation without a separate runtime installer.
- Install per user with desktop/Start menu shortcuts, an uninstall entry and a stable upgrade identity.
- Run existing checks and a read-only packaged startup check before compiling, and generate a SHA-256 checksum for GitHub Releases.
- Keep release runtime-pack restore separate from the existing development NuGet configuration.
- The user successfully compiled the installer with Inno Setup 6.7.3, passing the build script's checks and packaged read-only startup check. The generated SHA-256 checksum was verified; install/upgrade/uninstall testing is pending.

## 0.7.0 — Refined workspace UI

- Center the workspace with a bounded width for maximized windows and evenly sized display cards.
- Place lighting profiles beside screen arrangement, switching to a horizontal row below it in narrow windows.
- Replace percentage labels and explanatory copy with three vector sun icons with increasing fill and descriptive tooltips.
- Keep application progress, partial results and per-monitor details available after choosing a profile.
- Enlarge arrangement tiles, move keyboard guidance to a tooltip and theme sliders and scrollbars.
- Pass 123 automated/read-only checks and inspect wide, windowed, narrow and light-theme WPF layouts. The user approved this version for commit and push; further maximized-window refinement is requested.

## 0.6.0 — Lighting profiles

- Add Night, Normal and High light presets for all supported monitor brightness/contrast controls.
- Map percentages to hardware ranges and refresh capabilities before applying sequential writes.
- Skip unsupported, stale and unchanged controls; continue after individual failures.
- Verify read-back and report partial application, device-limited values and failures per monitor.
- Pass 123 automated/read-only checks, including simulated profile writes. Physical profile testing is pending.

## 0.5.0 — Compact workspace UI

- Embed screen arrangement above display settings in the main window.
- Replace oversized full-width display cards with a responsive one-to-three-column layout.
- Reduce headings, spacing and button sizes, and theme dropdowns for light and dark appearances.
- Preserve layout drafts during refresh when the Windows configuration is unchanged.
- Retain explicit apply actions and the existing 15-second preview rollback.
- Pass the existing 108 checks and inspect wide, narrow and light-theme layouts. The user approved the UI update for commit and push.

## 0.4.0 — Screen arrangement

- Add a draggable display layout editor with edge snapping, keyboard adjustment and draft reset.
- Preserve the primary display and reject overlaps, gaps and corner-only connections.
- Apply all positions in one temporary CCD configuration, preserving display modes and saved defaults.
- Reuse the 15-second confirmation flow to restore the captured layout on timeout or cancellation.
- Reject stale drafts and avoid restoring an obsolete topology after connection changes.
- Pass 108 automated/read-only checks; the user confirmed screen positioning works in the desktop application. Native validation from the agent sandbox remains blocked; individual recovery scenarios were not recorded separately.

## 0.3.0 — Display orientation

- Add landscape, portrait and flipped orientation previews with Windows driver validation.
- Preserve refresh rate and swap width/height on quarter turns.
- Include orientation in stale-state checks, apply verification and timed rollback.
- Label orientations for both native landscape and native portrait panels.
- Add regression coverage for all 16 orientation transitions and round trips.
- Pass 83 automated/read-only checks; user testing of orientation reported no issues.

## 0.2.0 — Display mode preview

- Add per-display resolution and refresh-rate selection from driver-enumerated modes.
- Validate mode changes with Windows before applying and re-read the resulting mode.
- Restore the previous mode after 15 seconds unless confirmed, or when the confirmation window closes.
- Run rollback independently of the UI dispatcher and report driver or disconnection failures.
- Keep confirmed modes for the current Windows session without updating registry defaults.
- Add mode filtering, error handling, timeout, disposal and confirmation-race checks.

## 0.1.2 — Reliable DDC/CI reads

- Pace hardware queries by 150 ms and retry transient communication errors up to three attempts.
- Retain previously read controls as disabled, clearly marked last-known values when a refresh fails.
- Clear cached values after a display disconnects and reject writes based on stale readings.
- Add read-only diagnostic modes and regression checks for retries, recovery and stale values.


## 0.1.1 — Display discovery recovery

- Ignore missing and malformed optional WMI brightness data.
- Keep external monitor discovery working when WMI runtime binding fails.
- Replace the loading heading with a clear failure state on discovery errors.
- Add regression checks for null provider values and failed-provider discovery.

## 0.1.0 — Initial preview

- Add active monitor discovery and resolution/refresh-rate information.
- Add capability-aware hardware brightness and contrast controls.
- Add WMI brightness support for compatible built-in panels.
- Add display identification, topology refresh, and light/dark themes.
- Add native ABI and input-validation checks plus Windows CI.

Resolution and refresh rate are read-only in this preview. Hardware writes still require manual validation.
