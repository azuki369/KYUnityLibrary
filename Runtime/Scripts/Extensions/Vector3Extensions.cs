using UnityEngine;


namespace KyLib.Extensions
{
    /// <summary>
    /// Vector3Šg’£ƒƒ\ƒbƒh
    /// </summary>
    public static class Vector3Extensions
    {

        public static bool IsNearZero(this Vector3 vec)
        {
            return vec.x < 0.0001f && vec.y < 0.0001f && vec.z < 0.0001f;
        }

        public static Vector3 XZ(this Vector3 vec)
        {
            return new Vector3(vec.x, 0.0f, vec.z);
        }

        public static Vector3 Y0(this Vector3 vec)
        {
            return new Vector3(vec.x, 0.0f, vec.z);
        }

    }
}
