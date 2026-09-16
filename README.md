# Tempo

A small native **Windows** timer, stopwatch, and work-hours tracker. Built as a single-file WinForms app on .NET Framework 4.x — no installer, no accounts, no cloud.

This repo is a public example of desktop UI work: custom drawing, an always-on-top overlay, and local-only persistence.

## Preview

<p align="center">
  <img src="screenshots/timer.png" alt="Tempo timer" width="420">
</p>

| Timer / Stopwatch / Work | Settings | Corner overlay |
| --- | --- | --- |
| <img src="screenshots/stopwatch.png" alt="Stopwatch" width="260"> | <img src="screenshots/settings.png" alt="Settings" width="260"> | <img src="screenshots/overlay.png" alt="Corner overlay" width="200"> |
| <img src="screenshots/work.png" alt="Work tracker" width="260"> | | |

## What it does

- **Timer** — presets or typed minutes/seconds, circular progress ring
- **Stopwatch** — running time with laps
- **Work** — session total that saves on this PC; named snapshots in **Saved timers**
- **Overlay** — a circular always-on-top widget while something is running (snaps to screen corners)
- **Settings** — turn idle auto-stop, mouse / keyboard / audio sensors, and the overlay on or off
- **Idle pause** — work mode can pause after a quiet stretch (mouse, keyboard, or playback audio)
- **Trim** — knock minutes off a work session if you forgot to pause

Work history stays in `%LocalAppData%\Tempo` on your machine. This repository does not contain timers, session names, or other personal data.

## Run

Use the included `Tempo.exe` on Windows, or build from source:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize+ /target:winexe /out:Tempo.exe /win32icon:Tempo.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.dll Tempo.cs
```

Needs a current Windows install (includes .NET Framework 4.x).

## Controls

| Key | Action |
| --- | --- |
| Space | Start / pause |
| R | Reset (Work: new work) |
| L | Lap (stopwatch) |

Drag the overlay to move it. Hover it for pause and end.

## Source

Everything lives in [`Tempo.cs`](Tempo.cs) — palette, overlay, work saves, and idle detection in one file on purpose, so the app stays easy to read and compile.
