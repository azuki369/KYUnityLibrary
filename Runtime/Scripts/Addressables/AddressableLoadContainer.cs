using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace KyLibrary.Addressables
{
    public sealed class AddressableLoadContainer : IDisposable
    {
        private readonly List<IAddressableLoadHandle> mHandles = new();

        public IReadOnlyList<IAddressableLoadHandle> Handles => mHandles;

        public int Count => mHandles.Count;

        public bool IsDone
        {
            get
            {
                for (int i = 0; i < mHandles.Count; i++)
                {
                    if (!mHandles[i].IsDone)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public float PercentComplete
        {
            get
            {
                if (mHandles.Count == 0)
                {
                    return 1.0f;
                }

                float total = 0.0f;
                for (int i = 0; i < mHandles.Count; i++)
                {
                    total += mHandles[i].PercentComplete;
                }

                return total / mHandles.Count;
            }
        }

        public AddressableAssetHandle<T> CreateAssetHandle<T>(string key)
        {
            AddressableAssetHandle<T> handle = new(key);
            mHandles.Add(handle);
            return handle;
        }

        public async UniTask<T> LoadAssetAsync<T>(string key, Action<T> onLoaded = null, IProgress<float> progress = null)
        {
            AddressableAssetHandle<T> handle = CreateAssetHandle<T>(key);
            T asset = await handle.LoadAsync(progress);
            onLoaded?.Invoke(asset);
            return asset;
        }

        public void ReleaseAll()
        {
            for (int i = mHandles.Count - 1; i >= 0; i--)
            {
                mHandles[i].Release();
            }

            mHandles.Clear();
        }

        public void Dispose()
        {
            ReleaseAll();
        }
    }
}
