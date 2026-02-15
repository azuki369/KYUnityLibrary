using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace KyLibrary
{
    public class CsvToJsonConverter : EditorWindow
    {
        // ----------------------------------------------------------------------------
        // 設定・定数
        // ----------------------------------------------------------------------------
        private static class Paths
        {
            public const string CSV_SRC_DIR = "Assets/MasterData/Csv";
            public const string JSON_OUT_DIR = "Assets/MasterData/Json";
            public const string SCRIPT_OUT_DIR = "Assets/Command01/Scripts/MasterData";
            // ID管理ファイルの保存場所
            public const string ID_MAP_FILE = "Assets/Submodules/MasterDataIdMap.json";
        }

        private static class RowConfig
        {
            // 行番号の定義 (0始まり)
            public const int EXTRA_SETTING = 1; // 2行目 (拡張設定: TestData.Label など)
            public const int COMMENT = 2; // 3行目 (日本語名)
            public const int VAR_NAME = 3; // 4行目 (変数名)
            public const int TYPE = 4; // 5行目 (型)
            public const int DATA_START = 5; // 6行目 (データ開始)
            public const int MIN_ROWS = 6; // 最小行数
        }

        // ----------------------------------------------------------------------------
        // データ構造定義
        // ----------------------------------------------------------------------------

        private class ColumnDefinition
        {
            public int Index;
            public string Name;
            public string Type;
            public string ExtraSetting;
        }

        /// <summary>
        /// マスタデータテーブル
        /// </summary>
        private class TableData
        {
            /// <summary> クラス名 </summary>
            public string ClassName;
            /// <summary> 列（変数名など） </summary>
            public List<ColumnDefinition> Columns = new List<ColumnDefinition>();
            /// <summary> 行（各ラベルのパラメータ） </summary>
            public List<Dictionary<string, object>> Rows = new List<Dictionary<string, object>>();
            /// <summary> ラベル </summary>
            public List<string> EnumLabels = new List<string>();
            /// <summary> ラベルに対応するユニークIDのリスト（順序はRowsと同じ） </summary>
            public List<int> UniqueIds = new List<int>();
        }

        [MenuItem("KYLib/MasterData/Convert CSV to JSON & Script")]
        public static void Convert()
        {
            // マスタデータなどを格納するディレクトリ確保
            EnsureDirectories();
            // IDマップの読み込み
            IdMapManager.Load();

            //CSVフォルダ内の.csvファイルパスを全て持ってくる
            string[] csvFiles = Directory.GetFiles(Paths.CSV_SRC_DIR, "*.csv");
            List<string> classNames = new List<string>();

            foreach (string filePath in csvFiles)
            {
                //最後のピリオドの前の文字列（ファイル名）を取得する
                string className = Path.GetFileNameWithoutExtension(filePath);
                var tableData = CsvParser.Parse(filePath, className);
                if (tableData != null)
                {
                    JsonGenerator.GenerateAndSave(tableData);
                    ScriptGenerator.GenerateAndSave(tableData);
                    classNames.Add(className);
                }
            }

            IdMapManager.Save();
            UpdateMasterDataRepository(classNames);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 必要なディレクトリがなかったら確保する
        /// </summary>
        private static void EnsureDirectories()
        {
            if (!Directory.Exists(Paths.CSV_SRC_DIR)) Directory.CreateDirectory(Paths.CSV_SRC_DIR);
            if (!Directory.Exists(Paths.JSON_OUT_DIR)) Directory.CreateDirectory(Paths.JSON_OUT_DIR);
            if (!Directory.Exists(Paths.SCRIPT_OUT_DIR)) Directory.CreateDirectory(Paths.SCRIPT_OUT_DIR);
        }


        // --- ID Map Manager (内部クラス) ---
        private static class IdMapManager
        {
            [System.Serializable]
            private class IdMapWrapper
            {
                public List<ClassMap> classes = new List<ClassMap>();
                public List<int> usedIds = new List<int>(); // 全体で使用済みのID
            }

            [System.Serializable]
            private class ClassMap
            {
                public string className;
                public List<Entry> entries = new List<Entry>();
            }

            [System.Serializable]
            private class Entry
            {
                public string label;
                public int id;
            }

            private static IdMapWrapper _data = new IdMapWrapper();
            private static HashSet<int> _usedIdsSet = new HashSet<int>();

            public static void Load()
            {
                if (File.Exists(Paths.ID_MAP_FILE))
                {
                    string json = File.ReadAllText(Paths.ID_MAP_FILE, Encoding.UTF8);
                    _data = JsonUtility.FromJson<IdMapWrapper>(json);
                    _usedIdsSet = new HashSet<int>(_data.usedIds);
                }
                else
                {
                    _data = new IdMapWrapper();
                    _usedIdsSet.Clear();
                }
            }

            public static void Save()
            {
                _data.usedIds = _usedIdsSet.ToList();
                string json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(Paths.ID_MAP_FILE, json, Encoding.UTF8);
            }

            /// <summary>
            /// ラベルに対応するIDを取得または新規発行する
            /// </summary>
            public static int GetOrGenerateId(string className, string label)
            {
                var classMap = _data.classes.FirstOrDefault(c => c.className == className);
                if (classMap == null)
                {
                    classMap = new ClassMap { className = className };
                    _data.classes.Add(classMap);
                }

                var entry = classMap.entries.FirstOrDefault(e => e.label == label);
                if (entry != null)
                {
                    return entry.id;
                }

                // 新規発行
                int newId = GenerateUniqueId();
                classMap.entries.Add(new Entry { label = label, id = newId });
                return newId;
            }

            private static int GenerateUniqueId()
            {
                // 簡易実装: ランダム生成 + 重複チェック
                // 実際には UniqueIdGenerator.Generate() を呼ぶ想定ですが、
                // エディタ拡張内で完結させるためここでロジックを持ちます。
                System.Random rand = new System.Random();
                int id;
                int safety = 0;
                do
                {
                    id = rand.Next(100000, 999999);
                    safety++;
                    if (safety > 10000) throw new System.Exception("ID generation failed.");
                } while (_usedIdsSet.Contains(id));

                _usedIdsSet.Add(id);
                return id;
            }
        }

        // --- Parser ---
        private static class CsvParser
        {
            public static TableData Parse(string filePath, string className)
            {
                string[] lines;
                //行ごとに文字列を読み込む
                try { lines = File.ReadAllLines(filePath, Encoding.UTF8); }
                catch { return null; }
                //マスタデータの要件を満たしていないものをはじく
                //ToDo:EnumOnlyでもできるようにする
                if (lines.Length < RowConfig.MIN_ROWS) return null;

                var table = new TableData { ClassName = className };

                //CSVを区切って分割して行ごとに確保
                string[] rawExtra = SplitCsvLine(lines[RowConfig.EXTRA_SETTING]);//追加設定
                string[] rawNames = SplitCsvLine(lines[RowConfig.VAR_NAME]);//フィールド名
                string[] rawTypes = SplitCsvLine(lines[RowConfig.TYPE]);//型
                //変数名の文字列を基に各列のフィールドの情報を設定
                for (int i = 1; i < rawNames.Length; i++)
                {
                    string vName = rawNames[i];
                    if (string.IsNullOrWhiteSpace(vName)) continue;

                    //列ごとのフィールドのパラメータデータ
                    var colDef = new ColumnDefinition
                    {
                        Index = i,
                        Name = vName.Trim(),
                        Type = (i < rawTypes.Length) ? rawTypes[i].Trim() : "string",
                        ExtraSetting = (i < rawExtra.Length) ? rawExtra[i].Trim() : ""
                    };
                    //フィールドデータを格納
                    table.Columns.Add(colDef);
                }

                if (table.Columns.Count == 0) return null;

                //データが記載されている行まで飛ばす
                for (int i = RowConfig.DATA_START; i < lines.Length; i++)
                {
                    //データのみが記載されている行を取得する
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    //データを一つずつ分割
                    string[] values = SplitCsvLine(line);
                    if (values.Length <= 1 || string.IsNullOrWhiteSpace(values[1])) continue;

                    // B列の値をラベル名として取得
                    string labelName = values[1].Trim();
                    table.EnumLabels.Add(labelName);

                    // ★ここでID割り当てを実行
                    int uniqueId = IdMapManager.GetOrGenerateId(className, labelName);
                    table.UniqueIds.Add(uniqueId);

                    var rowDict = new Dictionary<string, object>();
                    //列
                    foreach (var col in table.Columns)
                    {
                        //Indexの行の値を持ってくる
                        string valStr = (col.Index < values.Length) ? values[col.Index] : "";

                        // ★重要: B列（ID列）の値は、CSVの値ではなく「ユニークID」に差し替える
                        if (col.Index == 1) // B列と仮定
                        {
                            rowDict[col.Name] = uniqueId;
                        }
                        else
                        {
                            //指定の型に変換
                            rowDict[col.Name] = ParseValue(valStr, col.Type);
                        }
                    }
                    //一行分のデータを追加
                    table.Rows.Add(rowDict);
                }

                return table;
            }

            /// <summary>
            /// 値を引数の型に変換
            /// </summary>
            private static object ParseValue(string value, string type)
            {
                type = type.ToLower().Trim();
                value = value.Trim();
                if (type == "int") return int.TryParse(value, out int r) ? r : 0;
                if (type == "float") return float.TryParse(value, out float r) ? r : 0f;
                if (type == "bool") return (value.ToLower() == "true" || value == "1");
                if (type == "int[]") return ParseArrayInt(value);
                if (type == "string[]") return ParseArrayString(value);
                return value;
            }

            private static List<int> ParseArrayInt(string value)
            {
                var list = new List<int>();
                string stripped = value.Replace("\"", "");
                foreach (var s in stripped.Split(',')) if (int.TryParse(s.Trim(), out int v)) list.Add(v);
                return list;
            }

            private static List<string> ParseArrayString(string value)
            {
                var list = new List<string>();
                string stripped = value.Replace("\"", "");
                foreach (var s in stripped.Split(',')) if (!string.IsNullOrEmpty(s)) list.Add(s.Trim());
                return list;
            }

            /// <summary>
            /// 文字をダブルクォーテーションで区切って文字列を返す
            /// </summary>
            private static string[] SplitCsvLine(string line)
            {
                List<string> result = new List<string>();
                StringBuilder current = new StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    //文字がダブルクォーテーションだったら（文字列の区切り）
                    if (c == '\"')
                    {
                        inQuotes = !inQuotes;
                    }
                    //一区切りの部分でリストに追加
                    else if (c == ',' && !inQuotes)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else current.Append(c);
                }
                result.Add(current.ToString());
                return result.Select(s => s.Trim().Trim('\"').Replace("\"\"", "\"")).ToArray();
            }
        }

        // --- JsonGenerator ---
        private static class JsonGenerator
        {
            public static void GenerateAndSave(TableData table)
            {
                // Rowsの中身はすでにユニークIDに差し替わっている
                string jsonOutput = DictionaryListToJson(table.Rows);
                string jsonPath = Path.Combine(Paths.JSON_OUT_DIR, table.ClassName + ".json").Replace("\\", "/");
                File.WriteAllText(jsonPath, jsonOutput, Encoding.UTF8);
            }

            private static string DictionaryListToJson(List<Dictionary<string, object>> list)
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"list\": [");
                for (int i = 0; i < list.Count; i++)
                {
                    var dict = list[i];
                    var entries = new List<string>();
                    foreach (var kvp in dict)
                    {
                        string k = $"\"{kvp.Key}\"";
                        object v = kvp.Value;
                        string vStr;
                        if (v is string) vStr = $"\"{v}\"";
                        else if (v is bool) vStr = ((bool)v) ? "true" : "false";
                        else if (v is List<int> lInt) vStr = "[" + string.Join(",", lInt) + "]";
                        else if (v is List<string> lStr) vStr = "[" + string.Join(",", lStr.ConvertAll(s => $"\"{s}\"")) + "]";
                        else vStr = v.ToString();
                        entries.Add($"{k}: {vStr}");
                    }
                    sb.Append("    { " + string.Join(", ", entries) + " }");
                    if (i < list.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine("");
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                return sb.ToString();
            }
        }

        // --- ScriptGenerator (Region置換対応版) ---
        private static class ScriptGenerator
        {
            // マーカー定義
            private const string MARKER_ENUM_START = "// <AutoGenerated Enum>";
            private const string MARKER_ENUM_END = "// </AutoGenerated Enum>";

            private const string MARKER_FIELD_START = "// <AutoGenerated Fields>";
            private const string MARKER_FIELD_END = "// </AutoGenerated Fields>";

            private const string MARKER_PROP_START = "// <AutoGenerated Properties>";
            private const string MARKER_PROP_END = "// </AutoGenerated Properties>";

            private const string MARKER_METHOD_START = "// <AutoGenerated Methods>";
            private const string MARKER_METHOD_END = "// </AutoGenerated Methods>";

            public static void GenerateAndSave(TableData table)
            {
                string filePath = Path.Combine(Paths.SCRIPT_OUT_DIR, table.ClassName + ".cs").Replace("\\", "/");
                string content = "";

                // 各パーツのコード生成
                string codeEnum = GenerateEnumBlock(table);
                string codeFields = GenerateFieldsBlock(table);
                string codeProps = GeneratePropertiesBlock(table);
                string codeMethods = GenerateMethodsBlock(table);

                if (File.Exists(filePath))
                {
                    // 既存ファイルがある場合、中身を読み込んで置換
                    content = File.ReadAllText(filePath, Encoding.UTF8);
                    content = ReplaceBlock(content, MARKER_ENUM_START, MARKER_ENUM_END, codeEnum);
                    content = ReplaceBlock(content, MARKER_FIELD_START, MARKER_FIELD_END, codeFields);
                    content = ReplaceBlock(content, MARKER_PROP_START, MARKER_PROP_END, codeProps);
                    content = ReplaceBlock(content, MARKER_METHOD_START, MARKER_METHOD_END, codeMethods);
                }
                else
                {
                    // 新規作成の場合、テンプレートを作成
                    content = CreateTemplate(table.ClassName, codeEnum, codeFields, codeProps, codeMethods);
                }

                File.WriteAllText(filePath, content, Encoding.UTF8);
                Debug.Log($"Generated Script: {table.ClassName}.cs");
            }

            private static string CreateTemplate(string className, string codeEnum, string codeFields, string codeProps, string codeMethods)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine("using Cysharp.Threading.Tasks;");
                sb.AppendLine("using UnityEngine.AddressableAssets;");
                sb.AppendLine("");
                sb.AppendLine($"namespace KyLibrary");
                sb.AppendLine("{");
                sb.AppendLine("/// <summary>");
                sb.AppendLine($"/// {className} マスタデータ。");
                sb.AppendLine("/// </summary>");
                sb.AppendLine("[Serializable]");
                sb.AppendLine($"public class {className} : MasterDataBase<{className}, {className}.Label>");
                sb.AppendLine("{");

                sb.AppendLine($"    {MARKER_ENUM_START}");
                sb.Append(codeEnum);
                sb.AppendLine($"    {MARKER_ENUM_END}");
                sb.AppendLine("");

                sb.AppendLine($"    {MARKER_FIELD_START}");
                sb.Append(codeFields);
                sb.AppendLine($"    {MARKER_FIELD_END}");
                sb.AppendLine("");

                sb.AppendLine($"    {MARKER_PROP_START}");
                sb.Append(codeProps);
                sb.AppendLine($"    {MARKER_PROP_END}");
                sb.AppendLine("");

                sb.AppendLine($"    {MARKER_METHOD_START}");
                sb.Append(codeMethods);
                sb.AppendLine($"    {MARKER_METHOD_END}");

                sb.AppendLine("}");
                sb.AppendLine("}");
                return sb.ToString();
            }

            private static string ReplaceBlock(string original, string startMarker, string endMarker, string newContent)
            {
                int startIdx = original.IndexOf(startMarker);
                int endIdx = original.IndexOf(endMarker);

                if (startIdx == -1 || endIdx == -1 || startIdx >= endIdx)
                {
                    // マーカーが見つからない場合は、強制的に追記するか警告を出すなどの対応が必要だが、
                    // 今回は安全のため元のまま返す（ユーザーがマーカーを消した可能性がある）
                    Debug.LogWarning($"AutoGenerated markers not found or invalid in script. Skipping update for block: {startMarker}");
                    return original;
                }

                // マーカーの内側を置換
                // startMarkerの後ろの改行を含めて調整
                int contentStart = startIdx + startMarker.Length;

                // 改行コードを考慮して次の行頭を探す（簡易実装）
                if (original[contentStart] == '\r') contentStart++;
                if (original[contentStart] == '\n') contentStart++;

                string prefix = original.Substring(0, contentStart);
                string suffix = original.Substring(endIdx);

                return prefix + newContent + (newContent.EndsWith("\n") ? "" : "\n") + "    " + suffix;
            }

            // --- 各ブロックの生成ロジック ---

            private static string GenerateEnumBlock(TableData table)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("    public enum Label");
                sb.AppendLine("    {");

                HashSet<string> processed = new HashSet<string>();
                for (int i = 0; i < table.EnumLabels.Count; i++)
                {
                    string label = table.EnumLabels[i];
                    int id = table.UniqueIds[i];

                    if (processed.Contains(label)) continue;
                    processed.Add(label);

                    if (int.TryParse(label, out _))
                        sb.AppendLine($"        Key_{label} = {id},");
                    else
                        sb.AppendLine($"        {label} = {id},");
                }
                sb.AppendLine("    }");
                return sb.ToString();
            }

            private static string GenerateFieldsBlock(TableData table)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var col in table.Columns)
                {
                    string csType = GetCsType(col);
                    sb.AppendLine($"    [SerializeField] private {csType} {col.Name};");
                }
                return sb.ToString();
            }

            private static string GeneratePropertiesBlock(TableData table)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var col in table.Columns)
                {
                    string csType = GetCsType(col);
                    string propName = char.ToUpper(col.Name[0]) + col.Name.Substring(1);
                    if (propName == col.Name) propName += "Value";
                    sb.AppendLine($"    public {csType} {propName} => {col.Name};");
                }
                return sb.ToString();
            }

            private static string GenerateMethodsBlock(TableData table)
            {
                StringBuilder sb = new StringBuilder();
                string idColName = table.Columns[0].Name;

                sb.AppendLine($"    protected override Label GetKey({table.ClassName} data) => (Label)data.{idColName};");
                sb.AppendLine($"    protected override int GetId({table.ClassName} data) => (int)data.{idColName};");
                sb.AppendLine($"    public static {table.ClassName} GetData(Label label) => Get(label);");

                foreach (var col in table.Columns)
                {
                    if (!string.IsNullOrEmpty(col.ExtraSetting) && !IsStandardType(col.Type))
                    {
                        string target = col.Type;
                        string methodName = "Get" + char.ToUpper(col.Name[0]) + col.Name.Substring(1);
                        sb.AppendLine($"    public {target} {methodName}() => {target}.GetData(this.{col.Name});");
                    }
                }
                return sb.ToString();
            }

            private static string GetCsType(ColumnDefinition col) => !string.IsNullOrEmpty(col.ExtraSetting) ? col.ExtraSetting : col.Type;
            private static bool IsStandardType(string t) => t == "int" || t == "float" || t == "bool" || t == "string";
        }

        // --- RepositoryUpdater ---
        private static void UpdateMasterDataRepository(List<string> classNames)
        {
            string path = Path.Combine(Paths.SCRIPT_OUT_DIR, "MasterDataRepository.cs").Replace("\\", "/");

            if (!File.Exists(path))
            {
                CreateRepositoryTemplate(path, classNames);
                return;
            }

            string content = File.ReadAllText(path, Encoding.UTF8);
            // ※簡易実装: 辞書ブロックを探して追記するロジック (前回と同様)
            // ここでは省略せず記載すべきですが、長くなるため主要ロジックは前回通りとします
            // 実際には前回の UpdateMasterDataRepository メソッドを使用してください

            // （以下、前回のコードと同じUpdateロジック）
            string searchKey = "return new Dictionary<Type, IMasterData>";
            int dictStart = content.IndexOf(searchKey);
            if (dictStart == -1) return;
            int blockStart = content.IndexOf("{", dictStart);
            int blockEnd = content.IndexOf("};", blockStart);
            if (blockStart == -1 || blockEnd == -1) return;

            string currentBlock = content.Substring(blockStart, blockEnd - blockStart);
            StringBuilder insertion = new StringBuilder();
            bool isUpdated = false;

            foreach (var className in classNames)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(currentBlock, $@"typeof\s*\(\s*{className}\s*\)"))
                {
                    insertion.AppendLine($"            {{ typeof({className}), new {className}() }},");
                    isUpdated = true;
                }
            }

            if (isUpdated)
            {
                string newContent = content.Insert(blockEnd, insertion.ToString());
                File.WriteAllText(path, newContent, Encoding.UTF8);
            }
        }

        private static void CreateRepositoryTemplate(string path, List<string> classNames)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// =========================================================================");
            sb.AppendLine("// This file is auto-generated by CsvToJsonConverter (Template).");
            sb.AppendLine("// =========================================================================");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("");
            sb.AppendLine("public class MasterDataRepository : IMasterDataProvider");
            sb.AppendLine("{");
            sb.AppendLine("    public Dictionary<Type, IMasterData> GetMasters()");
            sb.AppendLine("    {");
            sb.AppendLine("        return new Dictionary<Type, IMasterData>");
            sb.AppendLine("        {");
            foreach (var className in classNames)
            {
                sb.AppendLine($"            {{ typeof({className}), new {className}() }},");
            }
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }

}
