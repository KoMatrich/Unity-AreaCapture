using UnityEngine;
using UnityEditor;

namespace AreaCapture.Editor
{
    [CustomEditor(typeof(CaptureZone))]
    public class CaptureZoneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var zone = (CaptureZone)target;

            EditorGUILayout.Space();

            if (Quaternion.Angle(zone.transform.rotation, Quaternion.identity) > 0.01f)
            {
                EditorGUILayout.HelpBox(
                    "This CaptureZone is rotated. Rotation is not supported — the captured image will not align correctly in the viewer app.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(4);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(new GUIContent("Quick Export (Last Settings)",
                "Exports this zone using the settings last saved in the Area Capture window."),
                GUILayout.Height(30)))
            {
                var settings = AreaCaptureExporter.LoadSettingsFromPrefs();
                AreaCaptureExporter.ExportZones(new[] { zone }, settings, success =>
                {
                    if (success)
                        EditorUtility.DisplayDialog("Success",
                            $"Export completed!\nFiles saved to: {settings.OutputDirectory}", "OK");
                    else
                        EditorUtility.DisplayDialog("Aborted",
                            "Export was canceled or failed. Check the Console for details.", "OK");
                });
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField(
                "Uses settings from the last Area Capture window session.",
                EditorStyles.centeredGreyMiniLabel);
        }
    }
}
