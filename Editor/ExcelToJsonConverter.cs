//using UnityEngine;
//using UnityEditor;
//using System.IO;
//using System.Collections.Generic;
//using System.Text;
//using System.Linq;

//// CSVを読み込んでJSONに変換するエディタ拡張
//public class CsvToJsonConverter : EditorWindow
//{
//    // CSVファイルの置き場所
//    private const string CSV_SRC_DIR = "Assets/MasterData/Csv";
//    // JSON出力先
//    private const string JSON_OUT_DIR = "Assets/MasterData/Json";

//    [MenuItem("Tools/MasterData/Convert CSV to JSON")]
//    public static void Convert()
//    {
//        if (!Directory.Exists(CSV_SRC_DIR))
//        {
//            Directory.CreateDirectory(CSV_SRC_DIR);
//            Debug.LogWarning($"CSV folder created. Please put your CSV files in: {CSV_SRC_DIR}");
//            return;
//        }
//        if (!Directory.Exists(JSON_OUT_DIR)) Directory.CreateDirectory(JSON_OUT_DIR);

//        string[] csvFiles = Directory.GetFiles(CSV_SRC_DIR, "*.csv");
//        if (csvFiles.Length == 0)
//        {
//            Debug.LogWarning("No CSV files found.");
//            return;
//        }

//        int successCount = 0;
//        foreach (string filePath in csvFiles)
//        {
//            if (ConvertSingleCsvToJson(filePath.Replace("\\", "/")))
//            {
//                successCount++;
//            }
//        }

//        AssetDatabase.Refresh();
//        Debug.Log($"Conversion complete! Generated {successCount} JSON files.");
//    }

//    private static bool ConvertSingleCsvToJson(string filePath)
//    {
//        string fileName = Path.GetFileNameWithoutExtension(filePath);

//        string[] lines;
//        try
//        {
//            // ExcelのCSV UTF-8に合わせてUTF8で読み込み
//            lines = File.ReadAllLines(filePath, Encoding.UTF8);
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Failed to read {fileName}: {e.Message}");
//            return false;
//        }

//        if (lines.Length < 6)
//        {
//            Debug.LogWarning($"{fileName}: Incomplete rows.");
//            return false;
//        }

//        // 行4(index 3): 変数名
//        string[] allVarNames = SplitCsvLine(lines[3]);
//        // 行5(index 4): 型定義
//        string[] allVarTypes = SplitCsvLine(lines[4]);

//        // B列(index 1)以降で、変数名が入っている列のインデックスを抽出
//        List<int> targetColumnIndices = new List<int>();
//        for (int i = 1; i < allVarNames.Length; i++)
//        {
//            if (i < allVarNames.Length && !string.IsNullOrWhiteSpace(allVarNames[i]))
//            {
//                targetColumnIndices.Add(i);
//            }
//        }

//        if (targetColumnIndices.Count == 0)
//        {
//            Debug.LogError($"{fileName}: No variable names found in row 4 starting from column B.");
//            return false;
//        }

//        List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();

//        // 行6(index 5)からデータ本体
//        for (int i = 5; i < lines.Length; i++)
//        {
//            string line = lines[i];
//            if (string.IsNullOrWhiteSpace(line)) continue;

//            string[] values = SplitCsvLine(line);

//            // B列(index 1)が空ならデータなしとみなす
//            if (values.Length <= 1 || string.IsNullOrWhiteSpace(values[1])) continue;

//            var rowDict = new Dictionary<string, object>();
//            foreach (int col in targetColumnIndices)
//            {
//                string name = allVarNames[col];
//                string valStr = (col < values.Length) ? values[col] : "";
//                string typeStr = (col < allVarTypes.Length) ? allVarTypes[col] : "string";

//                rowDict[name] = ParseValue(valStr, typeStr);
//            }
//            dataList.Add(rowDict);
//        }

//        string jsonOutput = DictionaryListToJson(dataList);
//        string outputPath = Path.Combine(JSON_OUT_DIR, fileName + ".json").Replace("\\", "/");

//        try
//        {
//            File.WriteAllText(outputPath, jsonOutput, Encoding.UTF8);
//            Debug.Log($"Generated: {outputPath} (Data count: {dataList.Count})");
//            return true;
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Failed to write {fileName}: {e.Message}");
//            return false;
//        }
//    }

//    private static object ParseValue(string value, string type)
//    {
//        type = type.ToLower().Trim();
//        value = value.Trim();

//        if (type == "int") return int.TryParse(value, out int r) ? r : 0;
//        if (type == "float") return float.TryParse(value, out float r) ? r : 0f;
//        if (type == "bool") return (value.ToLower() == "true" || value == "1");
//        if (type == "int[]") return ParseArrayInt(value);
//        if (type == "string[]") return ParseArrayString(value);

//        return value;
//    }

//    private static List<int> ParseArrayInt(string value)
//    {
//        var list = new List<int>();
//        string stripped = value.Replace("\"", "");
//        foreach (var s in stripped.Split(','))
//        {
//            if (int.TryParse(s.Trim(), out int v)) list.Add(v);
//        }
//        return list;
//    }

//    private static List<string> ParseArrayString(string value)
//    {
//        var list = new List<string>();
//        string stripped = value.Replace("\"", "");
//        foreach (var s in stripped.Split(','))
//        {
//            if (!string.IsNullOrEmpty(s)) list.Add(s.Trim());
//        }
//        return list;
//    }

//    private static string DictionaryListToJson(List<Dictionary<string, object>> list)
//    {
//        var jsonEntries = new List<string>();
//        foreach (var dict in list)
//        {
//            var fieldEntries = new List<string>();
//            foreach (var kvp in dict)
//            {
//                string key = $"\"{kvp.Key}\"";
//                object val = kvp.Value;
//                string valStr;

//                if (val is string) valStr = $"\"{val}\"";
//                else if (val is bool) valStr = ((bool)val) ? "true" : "false";
//                else if (val is List<int> lInt) valStr = "[" + string.Join(",", lInt) + "]";
//                else if (val is List<string> lStr) valStr = "[" + string.Join(",", lStr.ConvertAll(s => $"\"{s}\"")) + "]";
//                else valStr = val.ToString();

//                fieldEntries.Add($"{key}: {valStr}");
//            }
//            jsonEntries.Add("    { " + string.Join(", ", fieldEntries) + " }");
//        }
//        return "{\n  \"list\": [\n" + string.Join(",\n", jsonEntries) + "\n  ]\n}";
//    }

//    // 正規表現を使わず、カンマで確実に分割する
//    private static string[] SplitCsvLine(string line)
//    {
//        List<string> result = new List<string>();
//        StringBuilder current = new StringBuilder();
//        bool inQuotes = false;

//        for (int i = 0; i < line.Length; i++)
//        {
//            char c = line[i];
//            if (c == '\"')
//            {
//                inQuotes = !inQuotes;
//            }
//            else if (c == ',' && !inQuotes)
//            {
//                result.Add(current.ToString());
//                current.Clear();
//            }
//            else
//            {
//                current.Append(c);
//            }
//        }
//        result.Add(current.ToString());

//        return result.Select(s => s.Trim().Trim('\"').Replace("\"\"", "\"")).ToArray();
//    }
//}