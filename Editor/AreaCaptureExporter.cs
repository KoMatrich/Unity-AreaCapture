using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AreaCapture.Runtime;

using UnityEditor;

namespace AreaCapture.Editor
{
    /// <summary>
    /// Exports captured areas to PNG images and JSON metadata
    /// </summary>
    public class AreaCaptureExporter
    {
        internal const string PREF_KEY_PPU        = "AreaCapture_PPU";
        internal const string PREF_KEY_OUTDIR     = "AreaCapture_OutDir";
        internal const string PREF_KEY_META       = "AreaCapture_Meta";
        internal const string PREF_KEY_CLEARFLAG  = "AreaCapture_ClearFlag";
        internal const string PREF_KEY_BGCOLOR    = "AreaCapture_BGColor";
        internal const string PREF_KEY_CULLMASK   = "AreaCapture_CullMask";
        public static ExportSettings LoadSettingsFromPrefs()
        {
            var s = new ExportSettings
            {
                PixelPerUnit     = EditorPrefs.GetInt(PREF_KEY_PPU, 100),
                OutputDirectory  = EditorPrefs.GetString(PREF_KEY_OUTDIR, "Assets/Exports/AreaCaptures"),
                MetadataFilename = EditorPrefs.GetString(PREF_KEY_META, "capture_metadata.json"),
                ClearFlags       = (CameraClearFlags)EditorPrefs.GetInt(PREF_KEY_CLEARFLAG, (int)CameraClearFlags.SolidColor),
                CullingMask      = EditorPrefs.GetInt(PREF_KEY_CULLMASK, -1),
            };
            string html = EditorPrefs.GetString(PREF_KEY_BGCOLOR, "#00000000");
            if (ColorUtility.TryParseHtmlString(html, out Color c)) s.BackgroundColor = c;
            return s;
        }

        public class ExportSettings
        {
            public int PixelPerUnit;
            public string OutputDirectory;
            public string MetadataFilename;
            
            // Rendering options
            public CameraClearFlags ClearFlags = CameraClearFlags.SolidColor;
            public Color BackgroundColor = new Color(0, 0, 0, 0); // Transparent black by default
            public int CullingMask = -1; // Everything
        }

        /// <summary>
        /// Exports all CaptureZone components in the scene
        /// </summary>
        public static void ExportAllCaptureZones(ExportSettings settings = null, System.Action<bool> onComplete = null)
        {
            if (settings == null)
                settings = new ExportSettings();

            // Find all CaptureZone components in the scene
            CaptureZone[] zones = Object.FindObjectsByType<CaptureZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            if (zones.Length == 0)
            {
                Debug.LogWarning("No CaptureZone2D components found in the scene!");
                onComplete?.Invoke(false);
                return;
            }

            ExportZones(zones, settings, onComplete);
        }

        /// <summary>
        /// Exports specific CaptureZone components asynchronously to prevent Editor freezing
        /// </summary>
        public static void ExportZones(CaptureZone[] zones, ExportSettings settings = null, System.Action<bool> onComplete = null)
        {
            if (zones == null || zones.Length == 0)
            {
                onComplete?.Invoke(false);
                return;
            }
            
            if (settings == null)
                settings = new ExportSettings();

            // Create output directory
            if (!Directory.Exists(settings.OutputDirectory))
            {
                Directory.CreateDirectory(settings.OutputDirectory);
            }

            // Capture all zones using a state machine attached to EditorApplication.update
            var capturer = new RuntimeAreaCapture();
            var metadata = new CaptureMetadata();
            
            int currentIndex = 0;
            int faceIndex = 0; // 0-5 for cubemap faces
            int currentImageCount = 0;
            int totalImages = 0;
            foreach (var z in zones) totalImages += z.ExportCubemap ? 6 : 1;

            bool isInitializing = true;

            CaptureAxis[] cubemapAxes = new CaptureAxis[]
            {
                CaptureAxis.NegativeZ, // Front
                CaptureAxis.PositiveZ, // Back
                CaptureAxis.NegativeX, // Left
                CaptureAxis.PositiveX, // Right
                CaptureAxis.PositiveY, // Top
                CaptureAxis.NegativeY  // Bottom
            };

            string[] cubemapSuffixes = new string[] { "_Front", "_Back", "_Left", "_Right", "_Top", "_Bottom" };

            EditorApplication.CallbackFunction updateAction = null;
            updateAction = () =>
            {
                if (isInitializing)
                {
                    // Let Unity draw the requested progress bar for exactly 1 frame before freezing the thread with a capture
                    isInitializing = false;
                    return;
                }

                if (currentIndex >= zones.Length)
                {
                    // Finished
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update -= updateAction;

                    // Save metadata JSON
                    string jsonPath = Path.Combine(settings.OutputDirectory, settings.MetadataFilename);
                    SaveMetadataAsJson(metadata, jsonPath);

                    Debug.Log($"Export complete! Saved to: {settings.OutputDirectory}");
                    onComplete?.Invoke(true);
                    return;
                }

                var zone = zones[currentIndex];
                float totalProgress = (float)currentImageCount / totalImages;
                
                string processingName = GetZoneName(zone, currentIndex);
                
                CaptureAxis currentAxis = zone.ExportCubemap ? cubemapAxes[faceIndex] : zone.Axis;
                string suffix = zone.ExportCubemap ? cubemapSuffixes[faceIndex] : "_Top";

                bool canceled = EditorUtility.DisplayCancelableProgressBar(
                    "Exporting Area Captures", 
                    $"Capturing {processingName}{suffix} ({currentIndex + 1}/{zones.Length})...", 
                    totalProgress);

                if (canceled)
                {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update -= updateAction;
                    Debug.LogWarning("Capture export canceled by user.");
                    onComplete?.Invoke(false);
                    return;
                }

                var texture = capturer.CaptureArea(
                    zone, 
                    settings.PixelPerUnit, 
                    settings.ClearFlags, 
                    settings.BackgroundColor, 
                    settings.CullingMask,
                    currentAxis
                );

                if (texture == null)
                {
                    // Failed capture (e.g. out of memory)
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update -= updateAction;
                    capturer.Cleanup();
                    Debug.LogWarning($"Capture export aborted at '{processingName}' due to a rendering failure.");
                    onComplete?.Invoke(false);
                    return;
                }

                // Generate filename
                string fileName;
                if (!string.IsNullOrEmpty(zone.FilenameOverride) && !zone.ExportCubemap)
                {
                    fileName = Path.Combine(settings.OutputDirectory, zone.FilenameOverride);
                    if (!fileName.ToLower().EndsWith(".png")) fileName += ".png";
                }
                else
                {
                    string baseFileName = GetZoneFileName(zone, currentIndex, settings.OutputDirectory);
                    fileName = baseFileName.Replace(".png", suffix + ".png");
                }

                // Save PNG
                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(fileName, pngData);

                // Record metadata
                BoxCollider col = zone.GetComponent<BoxCollider>();
                Vector3 globalPos = col != null ? col.bounds.center : zone.GetGlobalPosition();
                Quaternion globalQuat = zone.transform.rotation;
                Vector3 size = col != null ? col.bounds.size : Vector3.zero;

                string faceName = zone.ExportCubemap ? cubemapSuffixes[faceIndex].TrimStart('_') : "Top";
                metadata.AddArea(processingName + suffix, Path.GetFileName(fileName), globalPos, globalQuat, size, faceName);

                Debug.Log($"Captured and saved: {fileName}");

                Object.DestroyImmediate(texture);

                currentImageCount++;

                if (zone.ExportCubemap)
                {
                    faceIndex++;
                    if (faceIndex >= 6)
                    {
                        faceIndex = 0;
                        currentIndex++;
                    }
                }
                else
                {
                    currentIndex++;
                }

                if (currentIndex >= zones.Length)
                {
                    // Clean up capturer resources
                    capturer.Cleanup();
                }
            };

            // Initial Progress Bar setup
            EditorUtility.DisplayProgressBar("Exporting Area Captures", "Initializing...", 0f);

            // Start the async process
            EditorApplication.update += updateAction;
        }

        private static string GetZoneFileName(CaptureZone zone, int index, string outputDir)
        {
            string zoneName = GetZoneName(zone, index);
            string fileName = $"CaptureZone_{zoneName}.png";
            return Path.Combine(outputDir, fileName);
        }

        private static string GetZoneName(CaptureZone zone, int index)
        {
            string rawName = string.IsNullOrEmpty(zone.name) ? $"Zone{index}" : zone.name;
            
            // Remove invalid file path characters and spaces
            var invalidChars = Path.GetInvalidFileNameChars();
            string cleanName = string.Join("_", rawName.Split(invalidChars, System.StringSplitOptions.RemoveEmptyEntries)).Replace(" ", "");
            
            return cleanName;
        }

        private static void SaveMetadataAsJson(CaptureMetadata metadata, string filePath)
        {
            // Manual JSON construction for proper formatting with numeric values
            var lines = new List<string> { "{" };
            var items = metadata.areas.ToList();
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var area = item.Value;
                
                lines.Add($"\t\"{item.Key}\": {{");
                lines.Add($"\t\t\"filename\": \"{area.filename}\",");
                lines.Add($"\t\t\"cubemap_face\": \"{area.cubemapFace}\",");
                lines.Add($"\t\t\"global_position\": {{ \"x\": {area.globalPosition.x:F2}, \"y\": {area.globalPosition.y:F2}, \"z\": {area.globalPosition.z:F2} }},");
                lines.Add($"\t\t\"global_quaternion\": {{ \"x\": {area.globalQuaternion.x:F4}, \"y\": {area.globalQuaternion.y:F4}, \"z\": {area.globalQuaternion.z:F4}, \"w\": {area.globalQuaternion.w:F4} }},");
                lines.Add($"\t\t\"size\": {{ \"x\": {area.size.x:F2}, \"y\": {area.size.y:F2}, \"z\": {area.size.z:F2} }}");
                lines.Add(i < items.Count - 1 ? "\t}," : "\t}");
            }
            
            lines.Add("}");
            string formattedJson = string.Join("\n", lines);

            File.WriteAllText(filePath, formattedJson);
        }

        // Helper class for JSON serialization
        [System.Serializable]
        private class JsonWrapper
        {
            public Dictionary<string, object> data;
        }
    }
}
