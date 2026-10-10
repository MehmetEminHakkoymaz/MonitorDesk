# Changelog

## 0.15.1 — Compact custom tray profiles (unreleased)

- Arrange custom profile buttons in three columns in the tray panel, matching preset button sizing; additional profiles continue onto new rows and long names remain available in tooltips.

## 0.15.0 — Custom monitor profiles (unreleased)

- Create named profiles with separate brightness/contrast percentages for each monitor; leave unchecked controls unchanged and save without applying.
- Persist custom profiles in a per-user JSON file using validated atomic replacement; restore them on launch and support editing, renaming and deletion.
- Show custom apply buttons in the main window and compact tray panel; retain disconnected monitor targets and match device identities/physical indices instead of display numbers.
- Reuse fresh capability reads, sequential hardware writes, stale/unsupported control guards and per-control read-back verification.
- Localize the editor and update both READMEs; pass 196 automated checks without hardware writes, including storage, validation, distinct monitor values and skipped/disconnected targets. Physical profile behavior requires user testing.
- Inspect the Turkish editor with fixed Save/Cancel actions and an isolated sample profile button in the tray panel without saving user data or changing monitors.

## 0.14.0 — Automatic monitor recovery (unreleased)

- Retry stale brightness/contrast reads automatically while open or hidden in the tray, targeting one affected monitor at a time and preserving healthy controls.
- Retry after 5 seconds, then wait 15, 30 and 60 seconds after failed attempts finish; stop on recovery or disconnection and skip unsupported controls without stale values.
- Defer recovery during mouse capture, hardware writes, profiles and display previews; serialize reads through the existing service gate and reserve UI controls during recovery.
- Restore current values and enabled sliders on success; update English/Turkish status messages and documentation. Diagnostics do not start background recovery.
- Pass 181 automated checks without hardware writes, including recovery deadlines, increasing backoff, cleanup and independent monitor schedules. Physical OMEN recovery requires user testing.

## 0.13.0 — Preferences and Windows startup (unreleased)

- Save theme, Eye comfort strength and normal window bounds/maximized state automatically in a per-user settings file; restore them on launch while keeping the warm filter off initially.
- Recover saved window placement inside a connected monitor's work area and handle monitor DPI when restoring size.
- Add a localized Preferences dialog with an opt-in current-user Windows startup entry that launches directly into the tray; suppress duplicate-instance warnings for automatic startup.
- Remove this installation's startup entry during uninstall and keep diagnostics from saving preferences or changing startup registration.
- Update both READMEs; pass 171 automated checks including atomic settings replacement, invalid data, disconnected-monitor geometry and mocked startup registration.
- Inspect Turkish/English settings layouts, verify hidden start and tray restore/close/exit without display writes, and compile the installer uninstall code. Real sign-in, restart persistence and mixed-DPI restoration require manual testing.

## 0.12.1 — Faster slider feedback (unreleased)

- Queue the first brightness/contrast change immediately instead of waiting for a quiet period.
- Sample the newest pending value at intervals of at least 250 ms per control while dragging; continuous input replaces intermediate values without postponing the dispatch deadline.
- Start the shared writer immediately when available, retain the final value after release, and preserve sequential commands and existing DDC retry/settle delays.
- Update English/Turkish tooltips and documentation; add immediate-first, continuous-drag and slow-write scheduling regression checks.
- Pass 155 automated checks without hardware writes. Physical response speed and OMEN reliability require user testing.

## 0.12.0 — Automatic brightness and contrast (unreleased)

- Apply brightness and contrast directly from sliders in the main window and tray panel; remove their Apply/checkmark buttons.
- Coalesce rapid input into the latest value per hardware control after a 250 ms pause, then write sequentially with existing DDC retry/settle pacing.
- Keep sliders available during writes and retain newer input arriving during a write/read-back. Rebuild monitor controls only after pending changes and mouse capture finish.
- Preserve hardware bounds and stale-control protection, read back actual monitor values, stop pending writes on an error, and discard unsent requests on explicit exit.
- Keep lighting profiles and display-mode previews from racing automatic changes; update English and Turkish status text and documentation.
- Pass 153 automated checks without hardware writes, including rapid-input coalescing, returning to the original value, separate control queues, WMI aliases and stale/range guards. Physical slider testing is pending.

## 0.11.0 — Automatic interface language (unreleased)

- Select Turkish for Turkish Windows display languages and English for other languages at startup.
- Share a translation catalogue across XAML, monitor settings, screen arrangement, lighting profiles, Eye comfort, tray controls/menu, preview confirmations, tooltips and app-generated status/error messages.
- Capture the language once so WPF callbacks and asynchronous display reads keep the same interface language; regional number/date formats remain independent.
- Keep service status identifiers, monitor names and native error codes intact; preserve unrecognized external error details.
- Update both READMEs and add culture-selection, fallback, formatting, callback and service-message regression checks.
- Pass 143 automated checks without hardware writes; inspect Turkish and English main-window snapshots plus Turkish compact-window and tray layouts.
- Build the self-contained v0.11.0 Windows x64 setup EXE with Inno Setup 6.7.3; pass its packaged read-only startup check and verify the generated SHA-256 checksum. Installation/upgrade/uninstall testing and GitHub Release upload remain separate steps.

## 0.10.2 — Duplicate launch protection (unreleased)

- Hold a per-user/session named mutex throughout the app lifetime, including tray hiding, and warn on duplicate normal launches before creating another window or filter.
- Release the lock on exit and recover ownership after an abandoned process; keep diagnostic runs isolated.
- Localize the duplicate-launch notification using the Windows UI culture: Turkish for Turkish locales, English otherwise.
- Display the selected Eye comfort intensity in the main window and tray toggle, updating it while enabled or disabled.
- Pass 121 existing checks plus separate-process lock contention/release checks and inspect the tray value display. Manual duplicate-launch dialog testing is pending.

## 0.10.1 — Smaller tray panel (unreleased)

- Reduce tray panel width from 390 to 320 logical pixels and maximum height from 800 to 560.
- Put each brightness/contrast control on one row with a labeled, keyboard-accessible checkmark apply button.
- Tighten monitor cards, use horizontal profile buttons and a smaller eye toggle; keep full status messages in a tooltip.
- Build successfully and verify the three-monitor layout and desktop open/hide/restore/exit lifecycle without monitor writes. The user approved this version for commit and push.

## 0.10.0 — Tray quick controls (unreleased)

- Add a single-left-click tray panel with vertically stacked monitor brightness/contrast controls and explicit apply actions.
- Place three lighting presets below monitors and an eye-shaped on/off warm-filter toggle below the presets.
- Share the main window's service, busy state, read-back and filter state; retain double-click/full-window restore and right-click exit.
- Hide the panel on outside clicks or Escape, cap its height to the monitor work area and position in native pixels for mixed-DPI desktops.
- Pass the existing 121 checks and desktop panel rendering/open/hide/restore/exit checks. The user approved this version for commit and push; physical slider/profile actions and mixed-DPI behavior have not been confirmed separately.

## 0.9.0 — System tray (unreleased)

- Hide the main window on close while keeping the app and warm filter running in the notification area.
- Add double-click restore and an Aç/Çıkış context menu; explicit exit removes the filter and disposes the tray icon.
- Preserve rollback of pending previews before hide/exit, normal minimization and Windows session shutdown.
- Keep diagnostic probe/snapshot runs independent of tray hiding and fall back to normal closing if tray initialization fails.
- Pass 121 automated/read-only checks and a desktop close/restore/process-exit lifecycle check. The user approved this version for commit and push; detailed manual menu and filter-retention tests have not been confirmed separately.

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
