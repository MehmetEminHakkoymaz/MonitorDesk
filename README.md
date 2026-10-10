# Monilivo

Previously named **MonitorDesk**. Existing settings and custom profiles remain in `%LOCALAPPDATA%\MonitorDesk` for compatibility. The installer keeps the same upgrade identity and updates old shortcuts/startup commands to `Monilivo.exe`; source folders and the GitHub repository retain their existing names. Use **Run-Monilivo.cmd** for local development.

**English** | [Türkçe](README.tr.md)

Fine-tune every screen. A lightweight Windows desktop app for reading display information and adjusting supported hardware brightness and contrast.

**Working title · v0.18.0 (development) · Windows only**

## Install

Download [Monilivo-Setup-0.18.0-win-x64.exe](artifacts/installers/Monilivo-Setup-0.18.0-win-x64.exe) from this repository using **Download raw file**, then run it. Its [SHA-256 checksum](artifacts/installers/Monilivo-Setup-0.18.0-win-x64.exe.sha256) is included. Older published installers are available on [GitHub Releases](https://github.com/MehmetEminHakkoymaz/MonitorDesk/releases).

The setup installs Monilivo for your Windows account, creates desktop and Start menu shortcuts, and adds an uninstall entry in Windows Settings. The .NET Windows Desktop runtime is bundled with the app, so no separate runtime installation or internet connection is needed on the target PC. Administrator rights are not requested. The package targets x64-compatible Windows 10 (build 19041+) and Windows 11; it contains no monitor drivers. Running a newer installer updates the same installation.

## Features

The app has its own monitor/light icon in the executable, main window and system tray. New installations use it for desktop/Start menu shortcuts and the setup EXE. The editable vector source is `src/MonitorDesk/Assets/MonitorDesk.svg`; regenerate its nine-size Windows icon with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-Icon.ps1`. Pinned shortcuts or Windows' icon cache may retain an older icon until the shortcut is recreated.

- Toggle an adjustable warm color overlay using **Eye comfort**; monitor brightness/contrast remain unchanged.

- Discover active displays, their current resolution, refresh rate and primary status.
- Identify screens with temporary numbered overlays.
- Read and adjust external monitor brightness and contrast using Windows DDC/CI APIs.
- Use Windows WMI brightness on compatible built-in panels.
- Show unavailable controls honestly, without simulated values or software dimming.
- Refresh after display configuration changes.
- Light and dark themes, per-monitor DPI awareness and keyboard-accessible controls.
- Adjust brightness and contrast directly with sliders in the main window and tray panel; changes apply automatically while dragging.
- Preview supported resolutions and refresh rates with a 15-second confirmation timeout.
- Preview landscape, portrait, landscape (flipped) and portrait (flipped) orientation.
- Arrange extended displays by dragging numbered tiles, with edge snapping and timed rollback.
- Apply Night, Normal and High light brightness/contrast profiles across supported monitors.

The main window combines the arrangement editor and compact display cards. Cards adapt to one, two or three columns as the window resizes, and dropdowns follow the selected light or dark theme. A draft arrangement survives refreshes while the underlying Windows layout is unchanged; configuration changes replace it with the current layout.

The workspace stays centered and capped at 1320 pixels when maximized. Lighting profiles sit beside screen arrangement in wider windows and move below it in narrower windows. Three sun icons have increasing fill for Night, Normal and High light; preset values are available in tooltips. Explanatory profile text is hidden until an operation needs a progress/result message. Sliders and scrollbars follow the selected theme.

### Preferences and Windows startup

Open **Preferences** from the top toolbar. Theme, Eye comfort strength, window position, size and maximized state are saved automatically to `%LOCALAPPDATA%\MonitorDesk\settings.json` and restored on the next launch. If a saved monitor is disconnected, the window is brought back into a connected screen's work area. Eye comfort remembers both its on/off state and selected strength, including when starting in the system tray with Windows.

Enable **Start with Windows in the system tray** to start Monilivo when your Windows account signs in, without opening the main window. This option is off by default and can be disabled in the same dialog. It registers the current executable in your account's Windows startup entries; enable it from the installed app for a stable executable path. Double-click the tray icon to open the main window. Uninstalling removes the startup entry belonging to that installation.

The development build passed 171 automated checks, including settings round trips, invalid data, disconnected-screen geometry and startup commands with mocked registry access. Turkish and English settings layouts and hidden-start/tray-restore behavior were checked. Real Windows sign-in, restart persistence and mixed-DPI restoration still need manual testing. Diagnostics do not save user preferences or enable startup.

### System tray

The app follows the Windows **display language** at startup: Turkish for Turkish locales, English otherwise. The main window, tray panel and menu, lighting profiles, tooltips, display-preview dialogs and app-generated status messages share this selection. Monitor names and external Windows/vendor error details remain as supplied. Restart Monilivo after changing the Windows display language; the regional date/number format alone does not switch the UI.

Only one normal Monilivo instance runs per Windows user/session. Launching it again while it is open or hidden in the tray shows an already-running message and exits the second copy. Explicit exit releases the lock; a terminated process does not leave a permanent lock. Diagnostic probes, snapshots and lifecycle checks are isolated from this startup guard. Exit any older app version before testing this feature, since versions before v0.10.2 do not acquire the lock.

Single-click the tray icon to toggle a compact quick-control panel. Monitors are stacked vertically with single-row brightness and contrast sliders that apply automatically. Hover over the status footer for the full message. Three lighting presets sit below the monitors, followed by an eye-shaped **Eye comfort · On/Off** toggle. It shares the main window's service, busy state and warm filter. Click outside or press Escape to hide the panel. The panel is 320 logical pixels wide, capped at 560 pixels high, and scrolls when needed. It opens above the taskbar on the clicked screen; double-click still opens the full window. The compact v0.10.1 build and desktop open/hide/restore/exit check passed; its three-monitor layout was visually inspected.

Closing the main window hides Monilivo in the Windows notification area. Double-click its icon, or right-click and choose **Aç** (Open), to restore the window. Choose **Çıkış** (Exit) to stop the app and remove the warm filter. The filter and display-change handling remain active while the window is hidden. Minimize still minimizes normally; closing an unconfirmed display preview restores it before hiding or exiting. Windows sign-out/shutdown ends the app normally. If tray initialization fails, closing exits so the app is not left running without an accessible icon.

The v0.9.0 build passed 121 automated/read-only checks and a desktop lifecycle check covering close-to-hide, restore and explicit process exit. Manual right-click menu, filter retention and Explorer restart checks remain pending.

### Automatic sliders

Previously readable controls that stop responding are retried automatically, including while the app is in the tray. Recovery reads only stale controls on one affected monitor at a time: first after 5 seconds, then 15, 30 and 60 seconds after unsuccessful attempts finish. Healthy and unsupported controls are not polled. Retries pause during slider interaction, writes, profiles and display previews; each recovery read temporarily reserves the shared controls to avoid competing commands. Successful reads restore current values and stop that monitor's retry schedule. Manual Refresh remains available. Native driver calls can still delay recovery or subsequent operations.

The v0.14.0 development build passed 181 automated checks without hardware writes, including increasing retry intervals, recovery/disconnection cleanup and independent monitor deadlines. Physical recovery on the OMEN monitor still needs user testing.

The first brightness/contrast change enters the hardware queue immediately. During dragging, the latest requested value is sampled at intervals of at least 250 ms per monitor/control; new input replaces pending intermediate values without postponing the dispatch deadline. Commands run sequentially with existing DDC recovery delays, so a busy or slower monitor can extend this interval. The final selected value remains queued after release. Both UI locations use the same queue. Sliders remain responsive during writes; read-back displays the actual value after input settles, so hardware-supported steps may differ from the request. Errors stop queued changes and report details; unavailable or stale controls stay disabled. Explicit exit discards unsent changes. Resolution, refresh rate, orientation and arrangement still use preview confirmation. The development build passed 155 automated checks without hardware writes; physical slider behavior needs user testing.

### Lighting profiles

Choose **+ New profile** to create a named custom profile with separate brightness and contrast percentages for each connected monitor. Uncheck a control to leave it unchanged. Saving stores the profile without applying it. Custom profiles appear as buttons below the three presets in the main window and tray panel; click a button to apply it. Use the pencil button in the main window to edit, rename or delete a profile. Values for disconnected monitors are retained when editing; monitors absent from the profile remain unchanged. Profiles match monitor device identities and physical indices rather than display numbers.

Custom profiles are stored in `%LOCALAPPDATA%\MonitorDesk\profiles.json` and restored on launch. Applying uses fresh capability reads, sequential writes and read-back verification; unavailable/stale controls are skipped. The v0.15.0 development build passed 196 automated checks, including persistence, invalid data, separate monitor values, excluded controls and disconnected devices. Real monitor profile application needs user testing.

Eye comfort shows its selected strength as **value / 90** below the main window slider and in the tray toggle, including while disabled. This is the filter's selected intensity level, not a percentage or a monitor brightness value. Changing the slider keeps both displays synchronized.

**Eye comfort** toggles a click-through amber overlay across the connected screens. Use its slider to adjust strength; turning it off or closing Monilivo removes the overlay. It is separate from Windows Night light and does not modify gamma calibration, monitor color temperature, brightness or contrast. Its on/off state and selected strength are saved between sessions. If it was on when Monilivo exited, the overlay is restored on the next launch, including Windows startup in the tray. An overlay blends a warm tint rather than reproducing Windows Night light's color transform; it may not cover exclusive fullscreen games or Windows secure desktop and can appear in screen captures. Physical interaction, mixed-DPI coverage and fullscreen behavior require user testing.

DDC reads now use increasing retry delays. Brightness/contrast writes retry transient transport failures up to three times, then wait for firmware to settle before another command or read-back. Unsupported commands are not retried and stale values remain explicitly marked. This addresses the reported transient contrast loss after brightness writes on the OMEN 25i; a fix on that physical monitor is not yet confirmed. The development build passed 121 automated/read-only checks without changing hardware settings.

Choose a button in **Lighting profiles** to immediately apply its preset to all connected monitors:

- **Night:** brightness 25%, contrast 60%.
- **Normal:** brightness 55%, contrast 70%.
- **High light:** brightness 90%, contrast 75%.

Values are percentages of each control's supported minimum-to-maximum range. Actual perceived brightness varies between monitors. Night adjusts hardware brightness and contrast; it does not change color temperature or enable Windows Night light. Profiles run only when chosen, with no schedule or automatic application on startup.

The app reads capabilities before applying, skips unavailable or stale controls, and sends supported settings sequentially. One failure does not block the remaining controls. Values already at the target are not written again. Read-back verifies results; **Results by monitor** shows successful changes, skipped controls, errors, unverified values and values that differ from the request. Built-in panels may snap brightness to supported steps. A profile can be partially applied; completed changes are retained. You can choose another profile or use the individual sliders afterward.

v0.7.0 passed 123 automated/read-only checks. Wide, windowed, narrow and light-theme WPF layouts were inspected. Profile batch tests use simulated writes; physical interaction with the updated UI requires user testing.

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
dotnet publish src/MonitorDesk -c Release --no-restore -o artifacts/Monilivo
```

Run `artifacts/Monilivo/Monilivo.exe`. This is a framework-dependent build: the target PC needs the .NET 10 Windows Desktop Runtime. Administrator rights are not requested.

### Build the setup EXE

The build machine needs the .NET 10 SDK, Inno Setup 6.3+ or 7, and internet access to download Microsoft's runtime packs. Install the compiler once:

```powershell
winget install --id JRSoftware.InnoSetup -e -s winget -i
```

Then run **Build-Installer.cmd** from the repository root, or:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Installer.ps1
```

The script runs the existing checks, publishes a self-contained Windows x64 app, performs a read-only startup check, and compiles `artifacts/installers/Monilivo-Setup-0.18.0-win-x64.exe` with a SHA-256 checksum alongside it. Only release publishing uses `installer/NuGet.Config` to obtain runtime packs; normal development keeps the existing package-source configuration. For a custom compiler location, pass `-CompilerPath "C:\path\to\ISCC.exe"`.

Before publishing, test installation, the desktop shortcut, updating an existing installation, and removal on a Windows account without .NET installed. Upload the EXE and `.sha256` file to a GitHub Release matching the app version. No release is created or uploaded by the local build script. The bundled runtime must be updated by rebuilding the installer with a patched SDK. The initial installer is unsigned; Windows may display an unknown-publisher warning.

The v0.11.0 setup EXE was built with Inno Setup 6.7.3 after passing 143 automated checks and its packaged read-only startup check. The generated SHA-256 checksum was verified. Installation, upgrade and uninstall testing are pending; the local EXE is not uploaded by a source-code push.

## Hardware limitations

Enable DDC/CI in the monitor's own menu. Some docks, adapters, drivers, HDR modes and monitor presets block or restrict controls. A readable value does not guarantee the device will accept writes. Automatic slider changes report command errors and read current values again.

WMI brightness is used only when an active provider matches the display device identity. Built-in panel brightness is snapped to a supported level. Contrast is not available through WMI. WMI and DDC/CI operations run off the UI thread and are serialized. A stuck driver call can delay further operations; a hard driver timeout is not implemented in this release.

Display numbers are local to this app, not guaranteed to match Windows Settings. Mirrored configurations may group physical screens. Refresh rate is the integer reported by EnumDisplaySettings (fractional rates are not represented).

### Resolution and refresh rate

Select a resolution and one of its supported refresh rates, then choose **Preview**. Windows validates the selected mode before applying it. Choose **Keep for this session** within 15 seconds, or use **Revert** / Escape to restore the previous mode. Closing the confirmation window also reverts. Confirmed changes apply to the current Windows session; saved Windows defaults are not overwritten.

Resolution and refresh-rate choices preserve the current color depth, orientation and scan type. Mirrored screens may change together. Display changes made elsewhere require a refresh before previewing. The rollback timer runs independently of the UI thread, but cannot recover from process termination or a hung display driver. A disconnected display or a driver failure may prevent restoration; errors are shown in the status area. Use Windows Display Settings if recovery is needed.

Use **Orientation** and **Rotate** to rotate the active mode. This keeps the current refresh rate and swaps pixel width and height for quarter turns; unapplied resolution selections are not used. Windows tests rotation support before making any change. The same 15-second confirmation restores both the previous orientation and resolution if you revert or do not confirm. Confirmation keeps the rotation for this Windows session only. Physical monitor rotation is manual. The user tested the orientation functionality on their setup and reported no issues; individual recovery scenarios were not recorded separately.

Local validation for v0.3.0 passed 83 checks including read-only mode enumeration on three displays. The user also tested the resolution and refresh-rate functionality on their setup and reported no issues. Individual manual recovery scenarios were not recorded separately.

### Screen arrangement

Use **Arrange your screens** at the top of the main window and drag the numbered tiles to match your desk. Nearby edges snap together. Use arrow keys to move a focused tile by 10 pixels, or Shift + arrows for 1-pixel adjustments. **Reset draft** restores the starting layout without changing Windows. The primary display remains primary; coordinates are normalized around it even when its tile is moved.

**Preview layout** is enabled for changed layouts with no overlapping screens or disconnected gaps. Screens must share an edge, not just a corner. Windows validates and applies all positions together, preserving the captured source and target modes. Confirm within 15 seconds to keep the layout for this Windows session, or revert to restore all original positions. Saved Windows defaults are not overwritten. Mirrored layouts are not supported; use Extend in Windows Settings first.

Connecting/disconnecting displays or changing display settings while editing invalidates the draft. A connection change during rollback is reported rather than applying an obsolete topology. As with mode previews, process termination or a hung driver can prevent automatic restoration; use Windows Display Settings if necessary.

v0.4.0 local validation passed 108 automated/read-only checks, including real CCD layout discovery. Native validation of the unchanged layout was skipped because the agent sandbox received Windows error 5 (access denied to the interactive display session). The user subsequently tested screen arrangement in the desktop application and confirmed that positioning works correctly. Individual timeout, disconnect and recovery scenarios were not recorded separately.

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
Monilivo.exe --probe C:\path\displays.json
Monilivo.exe --snapshot C:\path\window.png
```

## Roadmap

- Persist confirmed display modes across Windows sessions.
- Scheduled lighting changes.
- System tray controls and keyboard shortcuts.
- Additional interface languages and an optional manual language selector.
- Final product name and distributable installer.

This README is available in English and [Turkish](README.tr.md). Use the language links at the top of either file to switch. This changes the documentation language; the application UI independently follows the Windows display language (Turkish or English). Keep both README versions in sync when updating documentation. Code comments and GitHub activity use English.


### Intermittent contrast reads (v0.1.2)

On the local three-monitor setup, Display 2 returned an I2C transmission error (`0xC0262582`) in 6 of 8 back-to-back contrast reads. With a 150 ms pause after brightness, all 8 reads succeeded. This supports a timing-sensitive communication issue; it does not identify a specific cable, driver or firmware fault.

Hardware reads are now paced and transient transport failures are retried at most three times. Previously successful values remain visible but disabled and explicitly marked as not current when all attempts fail. No cached value is treated as a fresh reading. Cache entries are discarded when displays disappear. This improves resilience; it cannot guarantee driver or hardware reliability.

Read-only diagnostics (never change monitor settings):

```powershell
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose --paced
dotnet run --project tests/MonitorDesk.Checks -c Release -- --service-diagnose
```
