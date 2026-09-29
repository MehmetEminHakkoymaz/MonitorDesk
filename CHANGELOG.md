# Changelog

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
