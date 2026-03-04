using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace AreaCapture.Editor
{
    /// <summary>
    /// Editor window for capturing and exporting 2D areas
    /// </summary>
    public class AreaCaptureWindow : EditorWindow
    {
        private const string PREF_KEY_PPU = "AreaCapture_PPU";
        private const string PREF_KEY_OUTDIR = "AreaCapture_OutDir";
        private const string PREF_KEY_META = "AreaCapture_Meta";
        private const string PREF_KEY_CLEARFLAG = "AreaCapture_ClearFlag";
        private const string PREF_KEY_BGCOLOR = "AreaCapture_BGColor";
        private const string PREF_KEY_CULLMASK = "AreaCapture_CullMask";
        private const string PREF_KEY_AUTOIMPORT = "AreaCapture_AutoImport";

        private AreaCaptureExporter.ExportSettings settings;

        [MenuItem("Window/Area Capture")]
        public static void ShowWindow()
        {
            GetWindow<AreaCaptureWindow>("Area Capture");
        }

        private void OnEnable()
        {
            settings = new AreaCaptureExporter.ExportSettings
            {
                PixelPerUnit = EditorPrefs.GetInt(PREF_KEY_PPU, 100),
                OutputDirectory = EditorPrefs.GetString(PREF_KEY_OUTDIR, "Assets/Exports/AreaCaptures"),
                MetadataFilename = EditorPrefs.GetString(PREF_KEY_META, "capture_metadata.json"),
                ClearFlags = (CameraClearFlags)EditorPrefs.GetInt(PREF_KEY_CLEARFLAG, (int)CameraClearFlags.SolidColor),
                CullingMask = EditorPrefs.GetInt(PREF_KEY_CULLMASK, -1),
                AutoImportAssets = EditorPrefs.GetBool(PREF_KEY_AUTOIMPORT, false)
            };
            
            string colorHtml = EditorPrefs.GetString(PREF_KEY_BGCOLOR, "#00000000");
            if (ColorUtility.TryParseHtmlString(colorHtml, out Color loadedColor))
                settings.BackgroundColor = loadedColor;
        }

        private void OnDisable()
        {
            if (settings != null)
            {
                EditorPrefs.SetInt(PREF_KEY_PPU, settings.PixelPerUnit);
                EditorPrefs.SetString(PREF_KEY_OUTDIR, settings.OutputDirectory);
                EditorPrefs.SetString(PREF_KEY_META, settings.MetadataFilename);
                EditorPrefs.SetInt(PREF_KEY_CLEARFLAG, (int)settings.ClearFlags);
                EditorPrefs.SetInt(PREF_KEY_CULLMASK, settings.CullingMask);
                EditorPrefs.SetBool(PREF_KEY_AUTOIMPORT, settings.AutoImportAssets);
                EditorPrefs.SetString(PREF_KEY_BGCOLOR, "#" + ColorUtility.ToHtmlStringRGBA(settings.BackgroundColor));
            }
        }

        private HashSet<CaptureZone> selectedZones = new HashSet<CaptureZone>();
        private Vector2 zoneScrollPosition;

        private void OnGUI()
        {
            GUILayout.Label("2D Area Capture", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            GUILayout.Label("Capture Settings", EditorStyles.boldLabel);
            settings.PixelPerUnit = EditorGUILayout.IntField("Pixel Per Unit", settings.PixelPerUnit);

            EditorGUILayout.Space();

            GUILayout.Label("Rendering Options", EditorStyles.boldLabel);
            settings.ClearFlags = (CameraClearFlags)EditorGUILayout.EnumPopup("Clear Flags", settings.ClearFlags);
            
            if (settings.ClearFlags == CameraClearFlags.SolidColor)
            {
                settings.BackgroundColor = EditorGUILayout.ColorField("Background Color", settings.BackgroundColor);
            }

            settings.CullingMask = EditorGUILayout.MaskField("Culling Mask", 
                InternalEditorUtility.LayerMaskToConcatenatedLayersMask(new LayerMask { value = settings.CullingMask }), 
                InternalEditorUtility.layers);
            settings.CullingMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(settings.CullingMask).value;

            EditorGUILayout.Space();

            GUILayout.Label("Export Settings", EditorStyles.boldLabel);
            settings.OutputDirectory = EditorGUILayout.TextField("Output Directory", settings.OutputDirectory);
            if (GUILayout.Button("Browse..."))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Export Directory", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Convert to relative path
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        settings.OutputDirectory = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }

            settings.MetadataFilename = EditorGUILayout.TextField("Metadata Filename", settings.MetadataFilename);
            settings.AutoImportAssets = EditorGUILayout.Toggle("Auto Import Assets", settings.AutoImportAssets);

            EditorGUILayout.Space();

            // Scene info
            GUILayout.Label("Scene Information", EditorStyles.boldLabel);
            CaptureZone[] zones = Object.FindObjectsByType<CaptureZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox($"Found {zones.Length} CaptureZone component(s) in the scene.", MessageType.Info);
            
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            if (GUILayout.Button("Select All", EditorStyles.miniButton))
            {
                foreach (var z in zones) selectedZones.Add(z);
            }
            if (GUILayout.Button("Deselect All", EditorStyles.miniButton))
            {
                selectedZones.Clear();
            }
            if (GUILayout.Button("Show/Hide Gizmos", EditorStyles.miniButton))
            {
                bool show = zones.Length > 0 && !zones[0].ShowGizmo;
                foreach (var z in zones) { z.ShowGizmo = show; EditorUtility.SetDirty(z); }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (zones.Length > 0)
            {
                EditorGUILayout.Space(5);
                zoneScrollPosition = EditorGUILayout.BeginScrollView(zoneScrollPosition, GUILayout.MaxHeight(400));
                foreach (var zone in zones)
                {
                    bool isSelected = selectedZones.Contains(zone);
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    if (newSelected != isSelected)
                    {
                        if (newSelected) selectedZones.Add(zone);
                        else selectedZones.Remove(zone);
                    }

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(zone.gameObject.name, EditorStyles.boldLabel);
                    string info = $"Axis: {zone.Axis}";
                    if (zone.ExportCubemap) info += " | [Cubemap]";
                    if (zone.UseStrictClipping) info += " | [Clipped]";
                    if (!string.IsNullOrEmpty(zone.FilenameOverride)) info += $" | Name: {zone.FilenameOverride}";
                    EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = zone.gameObject;
                        EditorGUIUtility.PingObject(zone.gameObject);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();

            // Export button
            GUI.backgroundColor = Color.green;
            string exportLabel = selectedZones.Count == 0 || selectedZones.Count == zones.Length
                ? "Export All Zones"
                : $"Export Selected ({selectedZones.Count})";

            if (GUILayout.Button(exportLabel, GUILayout.Height(40)))
            {
                ExportZones(selectedZones.Count > 0 ? System.Linq.Enumerable.ToArray(selectedZones) : zones);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            GUILayout.Label("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Add CaptureZone component to GameObjects you want to capture\n" +
                "2. Configure the capture size, position, and axis\n" +
                "3. Use the list above to select specific zones if needed\n" +
                "4. Click 'Export' to render and save files",
                MessageType.Info);
        }

        private void ExportZones(CaptureZone[] zonesToExport)
        {
            if (settings.PixelPerUnit <= 0)
            {
                EditorUtility.DisplayDialog("Invalid Settings", "PixelPerUnit must be greater than 0", "OK");
                return;
            }

            try
            {
                AreaCaptureExporter.ExportZones(zonesToExport, settings, success => 
                {
                    if (success)
                    {
                        EditorUtility.DisplayDialog("Success", $"Export completed! Files saved to:\n{settings.OutputDirectory}", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Aborted", "Export process was canceled or failed due to an error. Check the Console for details.", "OK");
                    }
                });
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Export failed:\n{ex.Message}", "OK");
                Debug.LogError($"Export error: {ex}");
            }
        }
    }
}
