using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using KyLibrary;

namespace KyLibrary
{
    /// <summary>
    /// 各プロジェクトで定義されるマスタデータ一覧を提供するインターフェース。
    /// これを定義することで、MasterDataManagerはプロジェクト固有の型に依存しなくなります。
    /// </summary>
    public interface IMasterDataProvider
    {
        Dictionary<Type, IMasterData> GetMasters();
    }

    /// <summary>
    /// 全てのマスターデータを保持・管理するクラス。
    /// 外部からセットされた IMasterDataProvider に基づいてロードを行います。
    /// </summary>
    public class MasterDataManager : Singleton<MasterDataManager>
    {
        // ロード済みのマスターデータを保持する辞書
        private static Dictionary<Type, IMasterData> _masters;


        public void Destroy()
        {

        }


        /// <summary>
        /// 全てのマスタデータをロードする（起動時に呼び出す）
        /// </summary>
        /// <param name="provider">自動生成された MasterDataRepository などのインスタンス</param>
        public async UniTask LoadAllAsync(IMasterDataProvider provider)
        {
            if (provider == null)
            {
                Debug.LogError("[MasterDataManager] Provider is null. Cannot load master data.");
                return;
            }

            _masters = new Dictionary<Type, IMasterData>();
            _masters.Clear();

            // 1. プロジェクト側のリポジトリから「ロードすべきリスト」を取得
            var repoDict = provider.GetMasters();

            var tasks = new List<UniTask>();

            foreach (var kvp in repoDict)
            {
                Type type = kvp.Key;
                IMasterData master = kvp.Value;

                // 2. マネージャーの辞書に登録
                if (!_masters.ContainsKey(type))
                {
                    _masters.Add(type, master);
                }

                // 3. 非同期ロードを開始
                tasks.Add(master.LoadAsync());
            }

            // 4. 全てのロード完了を待機
            await UniTask.WhenAll(tasks);

            Debug.Log($"[MasterDataManager] Loaded {_masters.Count} masters via {provider.GetType().Name}.");
        }

        /// <summary>
        /// 指定した型のマスターデータを取得する
        /// </summary>
        public T Get<T>() where T : class, IMasterData
        {
            if(_masters == null)
            {
                Debug.LogError("[MasterDataManager] Masters not loaded. Call LoadAllAsync first.");
                return null;
            }

            if (_masters.TryGetValue(typeof(T), out var master))
            {
                return master as T;
            }

            return null;
        }
    }

}