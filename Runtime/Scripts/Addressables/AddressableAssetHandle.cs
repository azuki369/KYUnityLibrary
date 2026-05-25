using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace KyLibrary.Addressables
{
    public sealed class AddressableAssetHandle<T> : IAddressableLoadHandle
    {
        private AsyncOperationHandle<T> mHandle;
        private bool mHasHandle;
        private bool mIsReleased;

        public AddressableAssetHandle(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public bool IsDone => mHasHandle && mHandle.IsDone;

        public bool IsValid => mHasHandle && mHandle.IsValid() && !mIsReleased;

        public float PercentComplete => IsValid ? mHandle.PercentComplete : 0.0f;

        public T Asset
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

        public async UniTask<T> LoadAsync(IProgress<float> progress = null)
        {
            if (IsValid)
            {
                await WaitForCompletionAsync(progress);
                return Asset;
            }

            mHandle = Addressables.LoadAssetAsync<T>(Key);
            mHasHandle = true;
            mIsReleased = false;

            await WaitForCompletionAsync(progress);

            if (mHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Exception exception = mHandle.OperationException;
                Release();
                throw exception ?? new InvalidOperationException($"Failed to load addressable asset: {Key}");
            }

            return mHandle.Result;
        }

        public void Release()
        {
            if (!IsValid)
            {
                return;
            }

            Addressables.Release(mHandle);
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
