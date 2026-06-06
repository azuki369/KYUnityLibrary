using System;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace KyLibrary.Addressables
{
    public sealed class AddressableSceneHandle : IAddressableLoadHandle
    {
        private readonly LoadSceneMode mLoadSceneMode;
        private readonly bool mActivateOnLoad;
        private readonly int mPriority;
        private AsyncOperationHandle<SceneInstance> mHandle;
        private bool mHasHandle;
        private bool mIsReleased;

        public AddressableSceneHandle(string key, LoadSceneMode loadSceneMode, bool activateOnLoad = true, int priority = 100)
        {
            Key = key;
            mLoadSceneMode = loadSceneMode;
            mActivateOnLoad = activateOnLoad;
            mPriority = priority;
        }

        public string Key { get; }

        public bool IsDone => mHasHandle && mHandle.IsDone;

        public bool IsValid => mHasHandle && mHandle.IsValid() && !mIsReleased;

        public float PercentComplete => IsValid ? mHandle.PercentComplete : 0.0f;

        public SceneInstance Scene
        {
            get
            {
                if (!IsValid || !mHandle.IsDone || mHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    return default;
                }

                return mHandle.Result;
            }
        }

        public async UniTask<SceneInstance> LoadAsync(IProgress<float> progress = null)
        {
            if (IsValid)
            {
                await WaitForCompletionAsync(progress);
                return Scene;
            }

            mHandle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(Key, mLoadSceneMode, mActivateOnLoad, mPriority);
            mHasHandle = true;
            mIsReleased = false;

            await WaitForCompletionAsync(progress);

            if (mHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Exception exception = mHandle.OperationException;
                Release();
                throw exception ?? new InvalidOperationException($"Failed to load addressable scene: {Key}");
            }

            return mHandle.Result;
        }

        public void Release()
        {
            if (!IsValid)
            {
                return;
            }

            UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(mHandle);
            mIsReleased = true;
        }

        public void Dispose()
        {
            Release();
        }

        private async UniTask WaitForCompletionAsync(IProgress<float> progress)
        {
            while (!mHandle.IsDone)
            {
                progress?.Report(mHandle.PercentComplete);
                await UniTask.Yield();
            }

            progress?.Report(1.0f);
        }
    }
}
