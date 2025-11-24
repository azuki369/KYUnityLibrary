using System;
using UnityEngine;


namespace KyLib.Sequence
{
    public class SequenceState
    {
        /***********************************************************************************
            field
        ***********************************************************************************/

        private float mElapsedTime;

        private Action mStatePrevious;
        private Action mStateThis;
        private Action mStateNext;

        private bool mIsChangedFirstUpdate = false;


        /***********************************************************************************
           public
        ***********************************************************************************/

        public bool IsElapsedTime(float time)
        {
            return mElapsedTime >= time;
        }

        public void UpdateSequence(float DeltaTime)
        {
            if (mStateThis != null)
            {
                if (mElapsedTime > 0.0f)
                {
                    mIsChangedFirstUpdate = true;
                }

                if (mStateNext != null)
                {
                    mStatePrevious = mStateThis;
                    mStateThis = mStateNext;
                    mStateNext = null;
                    mIsChangedFirstUpdate = false;
                    mElapsedTime = 0.0f;
                }

                mElapsedTime += DeltaTime;
                mStateThis?.Invoke();
            }
        }

        public void ChangeState(Action nextState)
        {
            mStateNext = nextState;
        }

        public bool IsCurrentState(string stateName)
        {
            return mStateThis?.Method.Name == stateName;
        }
    }
}


