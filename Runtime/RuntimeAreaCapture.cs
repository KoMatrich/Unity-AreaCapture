using UnityEngine;
using System.Collections.Generic;

namespace AreaCapture.Runtime
{
    /// <summary>
    /// Runtime capture system for 2D areas. Renders capture zones to textures.
    /// </summary>
    public class RuntimeAreaCapture
    {
        private Camera persistentCamera;
        private GameObject cameraHolder;

        private Camera GetOrCreateCamera()
        {
            if (persistentCamera != null) return persistentCamera;

            // Try to find existing hidden camera first
            GameObject existing = GameObject.Find("_CaptureCamera_Internal");
            if (existing != null)
            {
                cameraHolder = existing;
                persistentCamera = existing.GetComponent<Camera>();
                return persistentCamera;
            }

            cameraHolder = new GameObject("_CaptureCamera_Internal");
            cameraHolder.hideFlags = HideFlags.HideAndDontSave;
            persistentCamera = cameraHolder.AddComponent<Camera>();
            persistentCamera.enabled = false; // We use manual Render()
            persistentCamera.useOcclusionCulling = false; // Ignore occlusion mapping of objects
            TryAddUrpCameraData(cameraHolder);
            return persistentCamera;
        }

        private static void TryAddUrpCameraData(GameObject go)
        {
            const string typeName =
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
            var t = System.Type.GetType(typeName);
            if (t != null && go.GetComponent(t) == null)
                go.AddComponent(t);
        }

        public void Cleanup()
        {
            if (cameraHolder != null)
            {
                Object.DestroyImmediate(cameraHolder);
                persistentCamera = null;
                cameraHolder = null;
            }
        }

        public Texture2D CaptureArea(CaptureZone captureZone, int pixelPerUnit, CameraClearFlags clearFlags = CameraClearFlags.SolidColor, Color backgroundColor = default, int cullingMask = -1, CaptureAxis? axisOverride = null)
        {
            if (captureZone == null)
                return null;

            CaptureAxis axis = axisOverride ?? captureZone.Axis;

            BoxCollider col = captureZone.GetComponent<BoxCollider>();
            if (col == null)
            {
                Debug.LogWarning($"CaptureZone on {captureZone.gameObject.name} is missing a BoxCollider.");
                return null;
            }

            Bounds bounds = col.bounds;
            Vector3 centerPos = bounds.center;
            Vector3 size = bounds.size;
            
            float orthoSize;
            float aspect;
            Vector3 cameraPos;
            Quaternion cameraRot;

            // Determine camera orientation and size based on capture axis
            switch (axis)
            {
                case CaptureAxis.PositiveX:
                    orthoSize = size.y * 0.5f;
                    aspect = size.z / size.y;
                    cameraPos = centerPos + captureZone.transform.right * (size.x * 0.5f + 10f);
                    cameraRot = Quaternion.LookRotation(-captureZone.transform.right, captureZone.transform.up);
                    break;
                case CaptureAxis.NegativeX:
                    orthoSize = size.y * 0.5f;
                    aspect = size.z / size.y;
                    cameraPos = centerPos + captureZone.transform.right * -(size.x * 0.5f + 10f);
                    cameraRot = Quaternion.LookRotation(captureZone.transform.right, captureZone.transform.up);
                    break;
                case CaptureAxis.PositiveY:
                    orthoSize = size.z * 0.5f;
                    aspect = size.x / size.z;
                    cameraPos = centerPos + captureZone.transform.up * (size.y * 0.5f + 10f);
                    cameraRot = Quaternion.LookRotation(-captureZone.transform.up, captureZone.transform.forward);
                    break;
                case CaptureAxis.NegativeY:
                    orthoSize = size.z * 0.5f;
                    aspect = size.x / size.z;
                    cameraPos = centerPos + captureZone.transform.up * -(size.y * 0.5f + 10f);
                    cameraRot = Quaternion.LookRotation(captureZone.transform.up, captureZone.transform.forward);
                    break;
                case CaptureAxis.PositiveZ:
                    orthoSize = size.y * 0.5f;
                    aspect = size.x / size.y;
                    cameraPos = centerPos + captureZone.transform.forward * (size.z * 0.5f + 10f);
                    cameraRot = Quaternion.LookRotation(-captureZone.transform.forward, captureZone.transform.up);
                    break;
                case CaptureAxis.NegativeZ:
                default:
                    orthoSize = size.y * 0.5f;
                    aspect = size.x / size.y;
                    cameraPos = centerPos + captureZone.transform.forward * -(size.z * 0.5f + 10f);
                    cameraRot = Quaternion.LookRotation(captureZone.transform.forward, captureZone.transform.up);
                    break;
            }

            // Get persistent camera
            Camera cam = GetOrCreateCamera();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.aspect = aspect;

            // Apply camera visual settings
            cam.clearFlags = clearFlags;
            cam.backgroundColor = backgroundColor == default ? new Color(0, 0, 0, 0) : backgroundColor;
            cam.cullingMask = cullingMask;

            cam.transform.position = cameraPos;
            cam.transform.rotation = cameraRot;

            // Strict Clipping: show only stuff inside the box depth
            if (captureZone.UseStrictClipping)
            {
                float depth;
                switch (axis)
                {
                    case CaptureAxis.PositiveX:
                    case CaptureAxis.NegativeX: depth = size.x; break;
                    case CaptureAxis.PositiveY:
                    case CaptureAxis.NegativeY: depth = size.y; break;
                    default: depth = size.z; break;
                }
                cam.nearClipPlane = 10f - 0.01f;
                cam.farClipPlane = 10f + depth + 0.01f;
            }
            else
            {
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
            }

            // Calculate target resolution
            float widthUnits = (axis == CaptureAxis.PositiveX || axis == CaptureAxis.NegativeX) ? size.z : size.x;
            float heightUnits = (axis == CaptureAxis.PositiveY || axis == CaptureAxis.NegativeY) ? size.z : size.y;
            
            int width = Mathf.Max(1, Mathf.RoundToInt(widthUnits * pixelPerUnit));
            int height = Mathf.Max(1, Mathf.RoundToInt(heightUnits * pixelPerUnit));
            
            // Constrain by hardware maximum
            int maxResolution = SystemInfo.maxTextureSize;
            if (width > maxResolution || height > maxResolution)
            {
                Debug.LogError($"Capture resolution ({width}x{height}) exceeds system maximum. Try smaller PPU.");
                return null;
            }
            
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            // Get best models for render
            float savedLodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = float.MaxValue;
            cam.Render();
            QualitySettings.lodBias = savedLodBias;

            RenderTexture.active = rt;
            // Always use RGBA32 to ensure alpha consistency
            TextureFormat texFormat = TextureFormat.RGBA32;
                                      
            Texture2D tex = new Texture2D(width, height, texFormat, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            
            RenderTexture.active = null;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }

        public Dictionary<CaptureZone, Texture2D> CaptureAllZones(CaptureZone[] zones, int pixelPerUnit)
        {
            var results = new Dictionary<CaptureZone, Texture2D>();
            foreach (var zone in zones)
            {
                if (zone != null) results[zone] = CaptureArea(zone, pixelPerUnit);
            }
            return results;
        }
    }
}
