using UnityEngine;
#if NAUGHTY_ATTRIBUTES
using NaughtyAttributes;
#endif

namespace AreaCapture
{
    public enum CaptureAxis
    {
        PositiveX, // From +X looking towards -X
        NegativeX, // From -X looking towards +X
        PositiveY, // From +Y looking towards -Y
        NegativeY, // From -Y looking towards +Y
        PositiveZ, // From +Z looking towards -Z
        NegativeZ  // From -Z looking towards +Z
    }

    [RequireComponent(typeof(BoxCollider))]
    public class CaptureZone : MonoBehaviour
    {
        [SerializeField]
        private Color gizmoColor = new Color(0, 1, 0, 0.3f);

        [SerializeField]
        private bool showGizmo = true;

        [SerializeField]
        private bool exportCubemap = false;

#if NAUGHTY_ATTRIBUTES
        [HideIf(nameof(exportCubemap))]
#endif
        [SerializeField]
        private CaptureAxis captureAxis = CaptureAxis.NegativeZ;

#if NAUGHTY_ATTRIBUTES
        [InfoBox("Enable 'Export Cubemap' to capture all 6 faces. In cubemap mode, Capture Axis and Filename Override are ignored.", EInfoBoxType.Normal)]
#endif
        [SerializeField]
        private bool useStrictClipping = true;

#if NAUGHTY_ATTRIBUTES
        [HideIf(nameof(exportCubemap))]
#endif
        [SerializeField]
        private string filenameOverride = "";

        public CaptureAxis Axis => captureAxis;
        public bool ExportCubemap => exportCubemap;
        public bool UseStrictClipping => useStrictClipping;
        public string FilenameOverride { get => filenameOverride; set => filenameOverride = value; }
        public bool ShowGizmo { get => showGizmo; set => showGizmo = value; }

        public Vector3 GetGlobalPosition()
        {
            return transform.position;
        }

        public float GetGlobalRotation()
        {
            return transform.eulerAngles.z;
        }

        private void OnDrawGizmos()
        {
            DrawGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmo(true);
        }

        private void DrawGizmo(bool selected)
        {
            if (!showGizmo) return;

            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            Color color = gizmoColor;
            if (selected) color.a = Mathf.Min(1f, color.a * 2f);

            Gizmos.color = color;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawCube(col.center, col.size);

            Color wireColor = color;
            wireColor.a = selected ? 1f : 0.8f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(col.center, col.size);

            if (exportCubemap)
            {
                foreach (CaptureAxis ax in new[] {
                    CaptureAxis.PositiveX, CaptureAxis.NegativeX,
                    CaptureAxis.PositiveY, CaptureAxis.NegativeY,
                    CaptureAxis.PositiveZ, CaptureAxis.NegativeZ })
                    DrawArrowForAxis(col.center, col.size, ax);
            }
            else
            {
                DrawArrowForAxis(col.center, col.size, captureAxis);
            }

            Gizmos.matrix = oldMatrix;
        }

        private void DrawArrowForAxis(Vector3 center, Vector3 size, CaptureAxis axis)
        {
            Vector3 direction = Vector3.forward;
            float halfSize = 0f;

            switch (axis)
            {
                case CaptureAxis.PositiveX: direction = Vector3.right;   halfSize = size.x * 0.5f; break;
                case CaptureAxis.NegativeX: direction = Vector3.left;    halfSize = size.x * 0.5f; break;
                case CaptureAxis.PositiveY: direction = Vector3.up;      halfSize = size.y * 0.5f; break;
                case CaptureAxis.NegativeY: direction = Vector3.down;    halfSize = size.y * 0.5f; break;
                case CaptureAxis.PositiveZ: direction = Vector3.forward; halfSize = size.z * 0.5f; break;
                case CaptureAxis.NegativeZ: direction = Vector3.back;    halfSize = size.z * 0.5f; break;
            }

            float padding     = 0.2f;
            float arrowLength = 1f;
            float arrowWidth  = 0.05f;
            float headSize    = 0.2f;

            Vector3 start = center + direction * (halfSize + padding + arrowLength);
            Vector3 end   = center + direction * (halfSize + padding);

            Gizmos.DrawLine(start, end);

            Vector3 up   = (direction == Vector3.up || direction == Vector3.down) ? Vector3.forward : Vector3.up;
            Vector3 side = Vector3.Cross(direction, up).normalized;
            up           = Vector3.Cross(side, direction).normalized;

            Gizmos.DrawLine(end, end + (direction * headSize) + (up   * arrowWidth));
            Gizmos.DrawLine(end, end + (direction * headSize) - (up   * arrowWidth));
            Gizmos.DrawLine(end, end + (direction * headSize) + (side * arrowWidth));
            Gizmos.DrawLine(end, end + (direction * headSize) - (side * arrowWidth));
        }
    }
}
