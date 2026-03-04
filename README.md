# Area Capture Plugin

A Unity Package Manager (UPM) compatible plugin that captures 3D areas and exports them as PNG images with JSON metadata.

## Installation

You can add this package to your Unity project using the Unity Package Manager via a Git URL:

1. Open the Unity Package Manager (**Window > Package Manager**).
2. Click the **+** button in the top left corner.
3. Select **"Add package from git URL..."**.
4. Enter the URL of this repository.

## Features

- **CaptureZone Component**: Mark areas for capture by adding this component to GameObjects with a BoxCollider.
- **Editor Export Window**: User-friendly interface to export selected or all capture zones.
- **Interactive Preview**: Preview the capture result directly in the editor before exporting.
- **JSON Metadata**: Exports transform and size information for each captured area with numeric precision.
- **Configurable Output**: Customize capture resolution, background, and export directory.
- **Strict Clipping**: Option to clip objects outside the capture volume.
- **Cubemap Export**: Support for exporting cubemaps from capture zones.

## How to Use

### Step 1: Add CaptureZone Component

1. Select a GameObject in your scene (or create a new empty GameObject).
2. Add the `CaptureZone` component via the Inspector.
3. It will automatically add a `BoxCollider` if one isn't present.
4. Configure the capture size (via BoxCollider) and orientation (via the Axis property).

### Step 2: Open Area Capture Window

Go to **Window > Area Capture** in the Unity Editor menu.

### Step 3: Configure Export Settings

- **Pixel Per Unit**: Resolution density of the exported images.
- **Output Directory**: Where to save the exported files.
- **Metadata Filename**: Name of the JSON metadata file.

### Step 4: Export

1. Use the checkboxes in the list to select specific zones, or leave them all unchecked to export everything.
2. Click **"Preview"** on any zone to see what the capture will look like.
3. Click **"Export"** to render and save files.

### Example Output

**Example metadata.json:**
```json
{
	"CaptureArea": {
		"filename": "CaptureZone_CaptureArea.png",
		"global_position": { "x": 7.73, "y": 2.40, "z": 0.94 },
		"global_euler": { "x": 0.00, "y": 0.00, "z": 0.00 },
		"global_quaternion": { "x": 0.0000, "y": 0.0000, "z": 0.0000, "w": 1.0000 },
		"size": { "x": 26.86, "y": 9.86, "z": 2.88 },
		"is_cubemap": false,
		"cubemap_face": ""
	}
}
```

## Namespace

- Core classes: `AreaCapture` (e.g., `CaptureZone`, `CaptureMetadata`)
- Editor classes: `AreaCapture.Editor`

## Components

### CaptureZone
Marks a volume for capture.

**Properties:**
- `Axis`: Direction from which the area is captured.
- `Export Cubemap`: Toggle to export a cubemap instead of a 2D image.
- `Filename Override`: Optional custom name for the exported PNG.
- `Use Strict Clipping`: If enabled, only objects inside the volume are rendered.
- `Show Gizmo`: Display the capture volume in the scene view.

## Tips

- **Better Quality**: Increase "Pixel Per Unit" for higher resolution.
- **Filenames**: Use the "Filename Override" in the CaptureZone inspector for specific naming requirements.
- **Selective Export**: Use the checkboxes in the Area Capture window to only re-export changed areas.
- **Preview**: Always preview before a large export to ensure the culling mask and background color are correct.
