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
        public Quaternion globalQuaternion;
        public Vector3 size;
        public string cubemapFace;

        public AreaMetadata(string filename, Vector3 globalPosition, Quaternion globalQuaternion, Vector3 size, string cubemapFace = "Top")
        {
            this.filename = filename;
            this.globalPosition = globalPosition;
            this.globalQuaternion = globalQuaternion;
            this.size = size;
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

        public void AddArea(string name, string filename, Vector3 globalPosition, Quaternion globalQuaternion, Vector3 size, string cubemapFace = "Top")
        {
            areas[name] = new AreaMetadata(filename, globalPosition, globalQuaternion, size, cubemapFace);
        }

        public void Clear()
        {
            areas.Clear();
        }
    }
}
