using System;
using UnityEngine;

namespace KyLibrary
{
    /// <summary>
    /// シーンに配置されるオブジェクトの基底クラス
    /// </summary>
    public class Actor : MonoBehaviour, IUpdater
    {
        [Flags]
        private enum EUpdateState
        {
            None = 0,
            Start = 0 << 1,
            Update = 1 << 2,
            Destroyed = 1 << 3,
        }

        /*************************************************************************
            Field
        *************************************************************************/
        private EUpdateState mUpdateState = EUpdateState.None;


        /*************************************************************************
            Static
        *************************************************************************/


        /// <summary>
        /// GameObjectからActorを生成
        /// </summary>
        /// <typeparam name="T">アタッチするコンポーネントのクラス</typeparam>
        /// <param name="gameObject">プレファブ</param>
        /// <returns></returns>
        public static T Create<T>(GameObject gameObject) where T : Actor
        {
            GameObject actorObject = Instantiate(gameObject);
            T actor = actorObject.AddComponent<T>();

            if (actor == null)
            {
                DebugUtil.LogErrorFormat("Actorの生成に失敗しました。型:{0}", typeof(T).Name);
                Destroy(actorObject);
                return null;
            }

            //Updaterに登録
            Updater.GetInstance().RegisterUpdater(actor);

            //ステート変更
            actor.ChangeStartState();

            return actor;
        }

        /*************************************************************************
            Private
        *************************************************************************/

        private void ChangeStartState()
        {
            mUpdateState |= EUpdateState.Start;
        }

        /*************************************************************************
            Protected
        *************************************************************************/

        protected virtual void Entry()
        {
            mUpdateState |= EUpdateState.Update;
        }

        protected virtual void OnUpdate() { }

        protected virtual void OnLateUpdate() { }

        protected void Destroy()
        {
            if(!((mUpdateState & EUpdateState.Destroyed) == EUpdateState.Destroyed))
            {
                DebugUtil.LogErrorFormat("Actorは既に破棄されています。型:{0}", this.GetType().Name);
                return;
            }

            mUpdateState = EUpdateState.Destroyed;

            //Updaterから削除
            Updater.GetInstance().ReserveRemoveUpdater(this);

            EndPlay();

            Destroy(this.gameObject);
        }

        protected virtual void EndPlay()
        {

        }

        /*************************************************************************
            IUpdater
        *************************************************************************/

        void IUpdater.Tick()
        {
            if ((mUpdateState & EUpdateState.Start) == EUpdateState.Start)
            {
                Entry();
            }
            else if ((mUpdateState & EUpdateState.Update) == EUpdateState.Update)
            {
                OnUpdate();
            }
        }

        void IUpdater.LateTick()
        {
            if ((mUpdateState & EUpdateState.Update) == EUpdateState.Update)
            {
                OnLateUpdate();
            }
        }

        /*************************************************************************
            MonoBehaviour
        *************************************************************************/

        private void OnDestroy()
        {
            //if ((mUpdateState & EUpdateState.Destroyed) != EUpdateState.Destroyed)
            //{
            //    //Updaterから削除
            //    Updater.GetInstance().ReserveRemoveUpdater(this);
            //}
        }
    }
}
