using Unity.VisualScripting;
using UnityEngine;

namespace KyLibrary
{
    /// <summary>
    /// オブジェクトを生成するタイプのシングルトンMonoBehaviour基底クラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T m_instance = null;


        public static T GetInstance()
        {
            if (m_instance == null)
            {
                DebugUtil.LogErrorFormat("{0}はインスタンスが生成されずに使用されようとしています。", typeof(T).Name);
                return null;
            }

            return m_instance;
        }

        public static void DeleteInstance()
        {
            m_instance = null;
        }
        /// <summary>
        /// インスタンスを生成
        /// </summary>
        /// <param name="parent"></param>
        public static void CreateInctance(GameObject parent)
        {
            //親があるなら
            if (parent != null)
            {
                //親にアタッチ
                m_instance = parent.AddComponent<T>();
            }
            else
            {
                //オブジェクトとして作成
                GameObject obj = new GameObject(typeof(T).Name);
                m_instance = obj.AddComponent<T>();
            }
        }

    }
}
