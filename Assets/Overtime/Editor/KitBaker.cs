using UnityEditor;
using UnityEngine;

namespace Overtime.EditorTools
{
    /// <summary>Collects the meshes inside kit.glb into a KitLibrary asset the game can load at runtime.</summary>
    public static class KitBaker
    {
        [MenuItem("Overtime/Bake Kit")]
        public static string Bake()
        {
            const string src = "Assets/Overtime/Kit/kit.glb", dst = "Assets/Overtime/Resources/Kit.asset";
            var lib = AssetDatabase.LoadAssetAtPath<KitLibrary>(dst);
            if (lib == null) { lib = ScriptableObject.CreateInstance<KitLibrary>(); AssetDatabase.CreateAsset(lib, dst); }
            lib.entries.Clear();
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(src);
            if (root == null) return "kit.glb did not import (is glTFast installed?)";
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                lib.entries.Add(new KitLibrary.Entry { name = mf.gameObject.name, mesh = mf.sharedMesh });
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return $"baked {lib.entries.Count} meshes";
        }
    }
}
