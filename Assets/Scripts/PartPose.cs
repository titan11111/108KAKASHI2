using UnityEngine;

namespace Kakashi
{
    [System.Serializable]
    public class PartPose
    {
        public Vector3 offset;
        public float tilt, twist, scale = 1;

        public void Clamp()
        {
            offset.x = Mathf.Clamp(offset.x, -.6f, .6f);
            offset.y = Mathf.Clamp(offset.y, -.5f, .5f);
            offset.z = Mathf.Clamp(offset.z, -.4f, .4f);
            tilt = Mathf.Clamp(tilt, -45, 45);
            twist = Mathf.Clamp(twist, -90, 90);
            scale = Mathf.Clamp(scale, .6f, 1.5f);
        }

        public void Reset() { offset = Vector3.zero; tilt = twist = 0; scale = 1; }
    }
}
