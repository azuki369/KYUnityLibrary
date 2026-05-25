using System;

namespace KyLibrary.Addressables
{
    public interface IAddressableLoadHandle : IDisposable
    {
        string Key { get; }

        bool IsDone { get; }

        bool IsValid { get; }

        float PercentComplete { get; }

        void Release();
    }
}
