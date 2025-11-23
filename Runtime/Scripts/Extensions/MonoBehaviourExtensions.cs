using UnityEngine;


namespace KyLib.Extensions
{
    /// <summary>
    /// MonoBehaviour拡張メソッド
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        public static MonoBehaviour SafeGetComponemt<T>(this MonoBehaviour monoBehaviour) where T : MonoBehaviour
        {
            T component = monoBehaviour.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[{monoBehaviour.gameObject.name}]が見つかりません。");
                return null;
            }
            return component;
        }
    }
}
