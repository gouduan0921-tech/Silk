using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HuaShang.Data;
using HuaShang.Game;
using HuaShang.Probe;
using HuaShang.Stations;
using System.Collections.Generic;

namespace HuaShang.EditorTools
{
    /// <summary>用代码生成场景，保证场景可以重建、可审阅。</summary>
    public static class SceneBuilder
    {
        public const string SceneFolder = "Assets/HuaShang/Scenes";
        public const string ProbeScene = SceneFolder + "/HS_Probe.unity";

        [MenuItem("HuaShang/场景/生成 HS_Probe（P1 灰盒探针）")]
        public static void BuildProbe()
        {
            EnsureFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var boot = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            boot.database = AssetDatabase.LoadAssetAtPath<ConfigDatabase>(GameConfig.DatabasePath);
            if (boot.database == null) Debug.LogError("[HuaShang] 找不到配置总表，请先导入。");
            new GameObject("ProbeBootstrap").AddComponent<ProbeBootstrap>();
            EditorSceneManager.SaveScene(scene, ProbeScene);
            Debug.Log("[HuaShang] 已生成 " + ProbeScene);
        }

        public const string ChainScene = SceneFolder + "/HS_Chain.unity";

        /// <summary>首发工位链场景：会话、工位链与首发时间轴。</summary>
        [MenuItem("HuaShang/场景/生成 HS_Chain（工位链）")]
        public static void BuildChain()
        {
            EnsureFolder();
            var timelines = TimelineBuilder.BuildAll();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HuaShang");
            var session = root.AddComponent<GameSession>();
            session.database = AssetDatabase.LoadAssetAtPath<ConfigDatabase>(GameConfig.DatabasePath);
            var boot = root.AddComponent<ChainBootstrap>();
            boot.timelines = timelines;
            EditorSceneManager.SaveScene(scene, ChainScene);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ChainScene, true),
                new EditorBuildSettingsScene(ProbeScene, true),
            };
            Debug.Log("[HuaShang] 已生成 " + ChainScene + "，时间轴 " + timelines.Count + " 条");
        }

        public static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets/HuaShang", "Scenes");
        }
    }
}
