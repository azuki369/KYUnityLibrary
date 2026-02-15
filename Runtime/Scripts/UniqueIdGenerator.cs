using System;
using System.Collections.Generic;

namespace KyLibrary
{

    /// <summary>
    /// 重複しないIDを生成・管理する汎用クラス
    /// </summary>
    public static class UniqueIdGenerator
    {
        private static readonly HashSet<int> _usedIds = new HashSet<int>();
        private static readonly Random _random = new Random();

        // IDの最小値と最大値の定義（用途に合わせて調整可能）
        private const int MIN_ID = 100000;
        private const int MAX_ID = 999999;

        /// <summary>
        /// 未使用の新しいIDを生成します
        /// </summary>
        public static int Generate()
        {
            int newId;
            int safetyNet = 0;

            do
            {
                newId = _random.Next(MIN_ID, MAX_ID);
                safetyNet++;
                if (safetyNet > 10000) throw new Exception("ID generation limit reached.");
            }
            while (_usedIds.Contains(newId));

            _usedIds.Add(newId);
            return newId;
        }

        /// <summary>
        /// 既存のIDを「使用済み」として登録します（マスタデータのロード時などに使用）
        /// </summary>
        public static void Register(int id)
        {
            _usedIds.Add(id);
        }

        /// <summary>
        /// 管理しているIDをリセットします
        /// </summary>
        public static void Clear()
        {
            _usedIds.Clear();
        }
    }
}
