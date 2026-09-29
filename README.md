# MonitorDesk

Fine-tune every screen. A lightweight Windows desktop app for reading display information and adjusting supported hardware brightness and contrast.

**Working title · v0.2.0 · Windows only**

## Features

- Discover active displays, their current resolution, refresh rate and primary status.
- Identify screens with temporary numbered overlays.
- Read and adjust external monitor brightness and contrast using Windows DDC/CI APIs.
- Use Windows WMI brightness on compatible built-in panels.
- Show unavailable controls honestly, without simulated values or software dimming.
- Refresh after display configuration changes.
- Light and dark themes, per-monitor DPI awareness and keyboard-accessible controls.
- Apply changes explicitly; moving a slider alone does not alter the monitor.
- Preview supported resolutions and refresh rates with a 15-second confirmation timeout.

## Build and run

Install the .NET 10 SDK on Windows, then run from the repository root:

```powershell
dotnet restore tests/MonitorDesk.Checks --configfile NuGet.Config
dotnet build tests/MonitorDesk.Checks -c Release --no-restore
dotnet run --project tests/MonitorDesk.Checks -c Release --no-build
dotnet run --project src/MonitorDesk -c Release --no-build
```

No third-party NuGet packages are required. NuGet.Config clears package feeds deliberately; the installed SDK supplies the framework references.

To create a distributable folder:

```powershell
dotnet publish src/MonitorDesk -c Release --no-restore -o artifacts/MonitorDesk
```

Run `artifacts/MonitorDesk/MonitorDesk.exe`. This is a framework-dependent build: the target PC needs the .NET 10 Windows Desktop Runtime. Administrator rights are not requested.

## Hardware limitations

Enable DDC/CI in the monitor's own menu. Some docks, adapters, drivers, HDR modes and monitor presets block or restrict controls. A readable value does not guarantee the device will accept writes. Apply reports command errors and reads current values again.

WMI brightness is used only when an active provider matches the display device identity. Built-in panel brightness is snapped to a supported level. Contrast is not available through WMI. WMI and DDC/CI operations run off the UI thread and are serialized. A stuck driver call can delay further operations; a hard driver timeout is not implemented in this release.

Display numbers are local to this app, not guaranteed to match Windows Settings. Mirrored configurations may group physical screens. Refresh rate is the integer reported by EnumDisplaySettings (fractional rates are not represented).

### Resolution and refresh rate

Select a resolution and one of its supported refresh rates, then choose **Preview mode**. Windows validates the selected mode before applying it. Choose **Keep for this session** within 15 seconds, or use **Revert** / Escape to restore the previous mode. Closing the confirmation window also reverts. Confirmed changes apply to the current Windows session; saved Windows defaults are not overwritten.

Choices preserve the current color depth, orientation and scan type. Mirrored screens may change together. Display changes made elsewhere require a refresh before previewing. The rollback timer runs independently of the UI thread, but cannot recover from process termination or a hung display driver. A disconnected display or a driver failure may prevent restoration; errors are shown in the status area. Use Windows Display Settings if recovery is needed.

Local validation passed 44 checks including read-only mode enumeration on three displays. The user also tested the resolution and refresh-rate functionality on their setup and reported no issues. Individual manual recovery scenarios were not recorded separately.

The app does not require an account, contact a server, or collect telemetry. Diagnostic exports contain local display identifiers; review them before sharing.

## Validation

Automated checks cover native structure layouts and rejecting unsupported or out-of-range writes before hardware access. A Windows CI workflow is prepared locally but is not enabled on GitHub yet. Automated checks cannot test real monitor behavior.

Local read-only validation detected two 1920×1080 external monitors at 240 Hz and 120 Hz. Both reported brightness; only the first reported contrast. Hardware writes and built-in panel WMI writes have not been tested.

Manual checks before a release:

- Apply a small brightness change on each supported display and restore it.
- Confirm unsupported contrast is unavailable.
- Identify screens with negative coordinates and different DPI scales.
- Disconnect/reconnect a display during refresh and verify recovery.
- Try light/dark themes, keyboard navigation and 125–200% scaling.
- Test a compatible laptop panel separately.

## Architecture

`Services/Native.cs` contains the Windows interop boundary. `Services/DisplayService.cs` owns capability reads, safe physical handle cleanup and serialized writes. `MainWindow` builds cards from actual capabilities; `App` also supports local read-only diagnostic modes:

```powershell
MonitorDesk.exe --probe C:\path\displays.json
MonitorDesk.exe --snapshot C:\path\window.png
```

## Roadmap

- Persist confirmed display modes across Windows sessions.
- Work, gaming and evening profiles.
- System tray controls and keyboard shortcuts.
- Localized UI resources and Turkish translation.
- Final product name and distributable installer.

Project documentation, code comments and GitHub activity use English.


### Intermittent contrast reads (v0.1.2)

On the local three-monitor setup, Display 2 returned an I2C transmission error (`0xC0262582`) in 6 of 8 back-to-back contrast reads. With a 150 ms pause after brightness, all 8 reads succeeded. This supports a timing-sensitive communication issue; it does not identify a specific cable, driver or firmware fault.

Hardware reads are now paced and transient transport failures are retried at most three times. Previously successful values remain visible but disabled and explicitly marked as not current when all attempts fail. No cached value is treated as a fresh reading. Cache entries are discarded when displays disappear. This improves resilience; it cannot guarantee driver or hardware reliability.

Read-only diagnostics (never change monitor settings):

```powershell
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose --paced
dotnet run --project tests/MonitorDesk.Checks -c Release -- --service-diagnose
```
