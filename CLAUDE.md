# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a Unity Package Manager (UPM) package (`com.komatrich.area-capture`) for capturing screenshots of designated 3D zones in a Unity scene and exporting them as PNG images with accompanying JSON metadata. It is an **editor-only tool** — users place `CaptureZone` components in their scene, open the `Window > Area Capture` editor window, configure settings, and click export.

## Development

This is a Unity package with no CLI build system. Development happens inside a Unity project that has this package installed (via git URL or local path). There are no test suites or lint scripts.

To install locally during development: add the package via **Package Manager > Add package from disk** and point to `package.json`, or use the git URL.

Optional dependency: **NaughtyAttributes** (`com.dbrizov.naughtyattributes`) — detected at compile time via `NAUGHTY_ATTRIBUTES` scripting define in the runtime asmdef. Code using NaughtyAttributes attributes must be wrapped in `#if NAUGHTY_ATTRIBUTES` guards.

## Architecture

### Assembly split

| Assembly | Folder | Available |
|---|---|---|
| `Com.Komatrich.AreaCapture` | `Runtime/` | Runtime + Editor |
| `Com.Komatrich.AreaCapture.Editor` | `Editor/` | Editor only |

### Data flow

```
AreaCaptureWindow (UI, EditorPrefs persistence)
  └─ AreaCaptureExporter.ExportZones(zones[], ExportSettings)
       └─ RuntimeAreaCapture.CaptureArea(zone, ppu, ...)   ← one call per zone/face
            └─ Internal orthographic Camera → RenderTexture → Texture2D
       └─ File.WriteAllBytes(path, texture.EncodeToPNG())
       └─ JSON metadata → File.WriteAllText
```

The exporter runs asynchronously via `EditorApplication.update` (state machine) to keep the editor responsive and show a cancelable progress bar.

### Key classes

- **`CaptureZone`** (`Runtime/CaptureZone.cs`) — `MonoBehaviour` + required `BoxCollider`. Stores axis direction, cubemap flag, strict-clipping flag, and filename override. Editor gizmo drawn in `OnDrawGizmos`.
- **`RuntimeAreaCapture`** (`Runtime/RuntimeAreaCapture.cs`) — Stateful renderer. Owns a hidden internal camera (orthographic, non-rendering by default). `CaptureArea()` sets camera position/rotation/planes per axis, renders to RenderTexture, reads back to `Texture2D`.
- **`AreaCaptureExporter`** (`Editor/AreaCaptureExporter.cs`) — Static export pipeline. For cubemap zones it iterates all 6 `CaptureAxis` values and appends `_Front`/`_Back`/etc. suffixes. Filenames fall back to `CaptureZone_{SanitizedName}.png` when no override is set.
- **`CaptureMetadata` / `AreaMetadata`** (`Runtime/CaptureMetadata.cs`) — Serializable POCOs written to JSON. JSON is built manually (not via `JsonUtility`) with controlled numeric precision (2 dp for position/size, 4 dp for quaternion).
- **`AreaCaptureWindow`** (`Editor/AreaCaptureWindow.cs`) — `EditorWindow` opened via `Window > Area Capture`. All settings persisted in `EditorPrefs`.

### Camera setup (RuntimeAreaCapture)

- Orthographic projection; size = half the perpendicular extent of the zone's `BoxCollider.size`
- Camera is offset 10 units back from the volume along the capture axis
- **Normal clipping**: near = 0.3, far = 1000
- **Strict clipping**: near = 10, far = 10 + depth of zone along capture axis (clips to exact volume bounds)
- Resolution = `sizeInUnits * pixelPerUnit`, clamped to `SystemInfo.maxTextureSize`

### CaptureAxis enum

Six values (`XPos`, `XNeg`, `YPos`, `YNeg`, `ZPos`, `ZNeg`) map to camera look directions and determine which two dimensions of the BoxCollider drive the output image resolution.
