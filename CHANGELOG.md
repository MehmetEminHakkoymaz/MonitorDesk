# Changelog

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
