using UnityEngine;
using KyLibrary;

namespace KyLib.Utility
{
    public class GameTimeManager : Singleton<GameTimeManager>
    {
        

        public void Initialize()
        {
            Debug.Log("GameTimeManager Initialized");
        }


        public GameTime GetType(EGameTimeType gameTimeType)
        {
            return null;
        }

    }
}


