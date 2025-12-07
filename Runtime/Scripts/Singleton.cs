using System.Dynamic;
using UnityEngine;

namespace KyLibrary
{
    /// <summary>
    /// オブジェクトを生成しないシングルトン基底クラス
    /// </summary>
    public class Singleton<T> where T : class
    {
        protected static T m_instance = null;


        public static void CreateInstance()
        {
            if (m_instance == null)
            {
                m_instance = System.Activator.CreateInstance<T>();
                DebugUtil.LogFormat("{0}のインスタンスを生成しました。", typeof(T).Name);
            }
        }

        public static T GetInstance()
        {
            if (m_instance == null)
            {
                DebugUtil.LogErrorFormat("{0}はインスタンスが生成されずに使用されようとしています。", typeof(T).Name);
                return null;
            }

            return m_instance;
        }

        public static void ClearInstance()
        {
            m_instance = null;
        }
    }
}

