using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace KyLibrary
{
    public interface IUpdater
    {
        void Tick();

        void LateTick();
    }

    /// <summary>
    /// IUpdater更新用クラス
    /// </summary>
    public class Updater : SingletonMonoBehaviour<Updater>
    {
        /*************************************************************************
            Field
        *************************************************************************/

        /// <summary> 更新インターフェースリスト </summary>
        private List<IUpdater> mUpdaterList = new List<IUpdater>();

        /// <summary> 更新削除インターフェースリスト </summary>
        private List<IUpdater> mRemoveUpdaterList = new List<IUpdater>();

        /*************************************************************************
            初期化
        *************************************************************************/

        public void Initialize()
        {
            DebugUtil.Log("[Updater] Initialized.");
        }

        public void Destroy()
        {
            DebugUtil.Log("[Updater] Destroyed.");
        }

        /*************************************************************************
            Updater
        *************************************************************************/

        /// <summary>
        /// Updaterを登録
        /// </summary>
        public void RegisterUpdater(IUpdater updater)
        {
            if (mUpdaterList.Contains(updater))
            {
                DebugUtil.LogWarning("既に登録されているインターフェースを再登録しています。");
            }

            mUpdaterList.Add(updater);
        }

        /// <summary>
        /// 登録されたUpdaterを削除予約
        /// </summary>
        public void ReserveRemoveUpdater(IUpdater updater)
        {
            if (!mUpdaterList.Contains(updater))
            {
                DebugUtil.LogWarning("登録されていないインターフェースを削除しようとしています。");
                return;
            }
            mRemoveUpdaterList.Add(updater);
        }

        /*************************************************************************
            MonoBehaviour
        *************************************************************************/

        void Start()
        {

        }

        
        void Update()
        {
            if (mUpdaterList.Count == 0)
            {
                return;
            }

            //更新中にリストの数が変わらないように今のリストを出す
            IUpdater[] updaters = mUpdaterList.ToArray();
            int count = updaters.Length;

            for (int i = 0; i < count; i++)
            {

                //Start処理が終わっているなら更新

                updaters[i].Tick();
            }

            ////削除予約されているUpdaterを削除

            //if (mRemoveUpdaterList.Count > 0)
            //{
            //    for (int i = 0; i < mRemoveUpdaterList.Count; i++)
            //    {
            //        mUpdaterList.Remove(mRemoveUpdaterList[i]);
            //    }
            //    mRemoveUpdaterList.Clear();
            //}
        }

        void LateUpdate()
        {
            //更新中にリストの数が変わらないように今のリストを出す
            IUpdater[] updaters = mUpdaterList.ToArray();
            int count = updaters.Length;

            for (int i = 0; i < count; i++)
            {

                //Start処理が終わっているなら更新

                updaters[i].LateTick();
            }

            //削除予約されているUpdaterを削除

            if (mRemoveUpdaterList.Count > 0)
            {
                for (int i = 0; i < mRemoveUpdaterList.Count; i++)
                {
                    mUpdaterList.Remove(mRemoveUpdaterList[i]);
                }
                mRemoveUpdaterList.Clear();
            }
        }
    }

}