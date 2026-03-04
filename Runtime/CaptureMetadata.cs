using UnityEngine;
using System.Collections.Generic;

namespace AreaCapture
{
    /// <summary>
    /// Metadata for a captured area including position, rotation, and size
    /// </summary>
    [System.Serializable]
    public class AreaMetadata
    {
        public string filename;
        public Vector3 globalPosition;
        public Vector3 globalEulerAngles;
        public Quaternion globalQuaternion;
        public Vector3 size;
        public bool isCubemap;
        public string cubemapFace;

        public AreaMetadata(string filename, Vector3 globalPosition, Vector3 globalEulerAngles, Quaternion globalQuaternion, Vector3 size, bool isCubemap = false, string cubemapFace = "")
        {
            this.filename = filename;
            this.globalPosition = globalPosition;
            this.globalEulerAngles = globalEulerAngles;
            this.globalQuaternion = globalQuaternion;
            this.size = size;
            this.isCubemap = isCubemap;
            this.cubemapFace = cubemapFace;
        }
    }

    /// <summary>
    /// Container for all captured areas' metadata
    /// </summary>
    [System.Serializable]
    public class CaptureMetadata
    {
        public Dictionary<string, AreaMetadata> areas = new Dictionary<string, AreaMetadata>();

        public void AddArea(string name, string filename, Vector3 globalPosition, Vector3 globalEulerAngles, Quaternion globalQuaternion, Vector3 size, bool isCubemap = false, string cubemapFace = "")
        {
            areas[name] = new AreaMetadata(filename, globalPosition, globalEulerAngles, globalQuaternion, size, isCubemap, cubemapFace);
        }

        public void Clear()
        {
            areas.Clear();
        }
    }
}
