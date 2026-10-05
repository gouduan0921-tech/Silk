using System.IO;
using UnityEditor;
using UnityEngine;
using HuaShang.Data;
using HuaShang.DocImport;
using HuaShang.Rules.Config;

namespace HuaShang.EditorTools
{
    /// <summary>
    /// 从 docs/ 生成 Assets/HuaShang/Data 下的配置表。改了手册就重跑一次；不在 Inspector 手改数值。
    /// </summary>
    public static class ConfigImporter
    {
        public const string DataFolder = "Assets/HuaShang/Data";

        [MenuItem("HuaShang/从 docs 导入配置表")]
        public static void ImportMenu()
        {
            var db = Import();
            if (db != null)
            {
                Selection.activeObject = db;
                EditorGUIUtility.PingObject(db);
            }
        }

        public static ConfigDatabase Import()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            ConfigSnapshot c;
            try
            {
                c = DocsConfigReader.ReadAll(root);
            }
            catch (DocParseException e)
            {
                Debug.LogError("[HuaShang 导入] 读手册失败：" + e.Message);
                return null;
            }

            var errors = ConfigValidator.Validate(c);
            foreach (var e in errors) Debug.LogError("[HuaShang 导入] 校验：" + e);
            if (errors.Count > 0) return null;

            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/HuaShang", "Data");

            var db = LoadOrCreate<ConfigDatabase>("ConfigDatabase");
            db.varieties = LoadOrCreate<VarietyTable>("VarietyTable"); db.varieties.rows = c.varieties;
            db.stretchGroups = LoadOrCreate<StretchGroupTable>("StretchGroupTable"); db.stretchGroups.rows = c.stretchGroups;
            db.dyes = LoadOrCreate<DyeTable>("DyeTable"); db.dyes.rows = c.dyes;
            db.patterns = LoadOrCreate<PatternTable>("PatternTable"); db.patterns.rows = c.patterns;
            db.characters = LoadOrCreate<CharacterTable>("CharacterTable"); db.characters.rows = c.characters;
            db.stages = LoadOrCreate<StageTable>("StageTable"); db.stages.rows = c.stages;
            db.quests = LoadOrCreate<QuestTable>("QuestTable"); db.quests.rows = c.quests;
            db.activities = LoadOrCreate<ActivityTable>("ActivityTable"); db.activities.rows = c.activities;
            db.unlocks = LoadOrCreate<UnlockTable>("UnlockTable"); db.unlocks.rows = c.unlocks;
            db.balance = LoadOrCreate<BalanceConstants>("BalanceConstants"); db.balance.balance = c.balance;
            db.opening = LoadOrCreate<OpeningSave>("OpeningSave"); db.opening.opening = c.opening;
            db.clothFixed = LoadOrCreate<ClothFixedSettings>("ClothFixedSettings"); db.clothFixed.values = c.clothFixed;

            foreach (var o in new ScriptableObject[]
                     {
                         db, db.varieties, db.stretchGroups, db.dyes, db.patterns, db.characters, db.stages,
                         db.quests, db.activities, db.unlocks, db.balance, db.opening, db.clothFixed,
                     })
                EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();

            foreach (var n in c.notes) Debug.LogWarning("[HuaShang 导入] 提示：" + n);
            Debug.Log("[HuaShang 导入] 完成：品种 " + c.varieties.Count + "，染料 " + c.dyes.Count
                      + "，角色 " + c.characters.Count + "，等级 " + c.unlocks.Count + "。");
            return db;
        }

        static T LoadOrCreate<T>(string name) where T : ScriptableObject
        {
            string path = DataFolder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
