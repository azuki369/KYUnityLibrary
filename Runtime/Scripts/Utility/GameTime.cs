using UnityEngine;

namespace KyLib.Utility
{
    public enum EGameTimeType
    {
        Root,

        Player,
        Enemy,
        Environment,
        UI,
        Other
    }

    /// <summary>
    /// ゲーム内時間を管理するクラス
    /// </summary>
    /// 
    public class GameTime : GameTimeBase
    {
        public GameTime(EGameTimeType gameTimeType) : base(gameTimeType)
        {

        }

        public static GameTime GetGameTime(EGameTimeType gameTimeType)
        {
            GameTime gameTime = GameTimeManager.GetInstance().GetType(gameTimeType);

            return new GameTime(gameTimeType);
        }

    }


    public class GameTimeBase
    {


        /****************************************************************************
            properties
        ****************************************************************************/

        public EGameTimeType GameTimeType { get; private set; }
        public float TimeScale { get;protected set; } = 1.0f;
        /// <summary>  </summary>
        public float DeltaTime => UnityEngine.Time.deltaTime * TimeScale;
        /// <summary>  </summary>
        public float UnscaledDeltaTime => UnityEngine.Time.deltaTime;
        /// <summary> 経過時間 </summary>
        public float Time { get; private set; }

        /****************************************************************************
            初期化関係
        ****************************************************************************/


        public GameTimeBase(EGameTimeType gameTimeType)
        {
            GameTimeType = gameTimeType;
        }

        /****************************************************************************
          public
        ****************************************************************************/

        public void SetTimeScale(float timeScale)
        {
            TimeScale = timeScale;
        }

    }
}
