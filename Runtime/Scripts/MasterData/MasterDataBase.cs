using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KyLibrary;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace KyLibrary
{
    public interface IMasterData
    {
        UniTask LoadAsync();
        void Load();
    }

    public class MasterDataContainer<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();
        private readonly Dictionary<int, TValue> _idMap = new Dictionary<int, TValue>();
        private readonly string _debugName;

        public MasterDataContainer(string debugName) => _debugName = debugName;

        public void Set(TKey key, int id, TValue value)
        {
            _map[key] = value;
            _idMap[id] = value;
            UniqueIdGenerator.Register(id);
        }

        public void Clear() => _map.Clear();

        public TValue Get(TKey key)
        {
            if (_map.TryGetValue(key, out var val)) return val;
            Debug.LogError($"[MasterData] Key not found: {key} in {_debugName}");
            return default;
        }

        // ID（int）で取得する関数を追加
        public TValue GetById(int id)
        {
            if (_idMap.TryGetValue(id, out var val)) return val;
            Debug.LogError($"[MasterData] ID not found: {id} in {_debugName}");
            return default;
        }

        public IEnumerable<TValue> GetAll() => _map.Values;
    }

    public abstract class MasterDataBase<TProvider, TKey> : IMasterData
        where TProvider : MasterDataBase<TProvider, TKey>, new()
    {
        protected static readonly MasterDataContainer<TKey, TProvider> Repository
            = new MasterDataContainer<TKey, TProvider>(typeof(TProvider).Name);

        [Serializable]
        private class Wrapper { public List<TProvider> list; }

        public static TProvider Get(TKey key) => Repository.Get(key);

        // 外部公開用のID取得関数
        public static TProvider GetById(int id) => Repository.GetById(id);

        public static IEnumerable<TProvider> GetAll() => Repository.GetAll();

        public async UniTask LoadAsync()
        {
            var asset = await Addressables.LoadAssetAsync<TextAsset>($"Assets/MasterData/Json/{typeof(TProvider).Name}.json").ToUniTask();
            ParseJson(asset.text);
        }

        public void Load()
        {
            var asset = Resources.Load<TextAsset>($"MasterData/{typeof(TProvider).Name}");
            if (asset != null) ParseJson(asset.text);
        }

        private void ParseJson(string json)
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(json);
            Repository.Clear();
            foreach (var item in wrapper.list)
            {
                Repository.Set(GetKey(item), GetId(item), (TProvider)item);
            }
        }

        protected abstract TKey GetKey(TProvider data);
        protected abstract int GetId(TProvider data); // ID取得用の抽象メソッド
    }
}
