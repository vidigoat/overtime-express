using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overtime.EditorTools
{
    /// <summary>One-off project setup and helpers for driving the editor from the command line.</summary>
    public static class DevTools
    {
        /// <summary>Make the Game view a fixed phone-portrait resolution.</summary>
        [MenuItem("Overtime/Phone Game View")]
        public static string PhoneView() => SetGameView(780, 1688, "Phone 780x1688");

        public static string SetGameView(int w, int h, string label)
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var inst = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(inst, new object[] { (int)sizesType.GetProperty("currentGroupType").GetValue(inst) });
            var gt = group.GetType();
            int total = (int)gt.GetMethod("GetTotalCount").Invoke(group, null), idx = -1;
            var texts = (string[])gt.GetMethod("GetDisplayTexts").Invoke(group, null);
            for (int i = 0; i < texts.Length; i++) if (texts[i].Contains(label)) idx = i;
            if (idx < 0)
            {
                var gvs = asm.GetType("UnityEditor.GameViewSize"); var gvst = asm.GetType("UnityEditor.GameViewSizeType");
                var size = gvs.GetConstructor(new[] { gvst, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { Enum.Parse(gvst, "FixedResolution"), w, h, label });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                idx = total;
            }
            var gvType = asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType, false, null, false);
            gvType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(gv, idx);
            gv.Repaint();
            return $"game view -> {label} (index {idx})";
        }

        /// <summary>Create the single empty scene the game boots from, and project settings for a portrait mobile game.</summary>
        [MenuItem("Overtime/Setup Project")]
        public static string Setup()
        {
            const string path = "Assets/Overtime/Main.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>(); cam.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            PlayerSettings.runInBackground = true;
            PlayerSettings.productName = "Overtime";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();
            return "scene + settings ready";
        }
    }
}
