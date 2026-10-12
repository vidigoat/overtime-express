using System.IO;
using UnityEditor;
using UnityEngine;

namespace Overtime.EditorTools
{
    /// <summary>Indexes every imported model under Art/ and the courier's animation clips into Resources/Art.asset.</summary>
    public static class ArtBaker
    {
        [MenuItem("Overtime/Bake Art")]
        public static string Bake()
        {
            const string dst = "Assets/Overtime/Resources/Art.asset";
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(dst);
            if (lib == null) { lib = ScriptableObject.CreateInstance<ArtLibrary>(); AssetDatabase.CreateAsset(lib, dst); }
            lib.models.Clear(); lib.clips.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Overtime/Art" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".glb") && !path.EndsWith(".gltf")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go) lib.models.Add(new ArtLibrary.Model { name = Path.GetFileNameWithoutExtension(path), prefab = go });
            }
            // the courier and every other person in the city share one humanoid body and one animation library
            foreach (var f in new[] { "UAL1", "UAL2" })
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath($"Assets/Overtime/Art/UAL/{f}.fbx"))
                    if (o is AnimationClip c && !c.name.StartsWith("__")) lib.clips.Add(c);
            foreach (var path in Directory.GetFiles("Assets/Overtime/Art/Chars", "*.fbx"))
                lib.models.Add(new ArtLibrary.Model { name = Path.GetFileNameWithoutExtension(path), prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) });
            lib.models.Add(new ArtLibrary.Model { name = "Courier", prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Overtime/Art/UAL/UAL2.fbx") });
            EditorUtility.SetDirty(lib); AssetDatabase.SaveAssets();
            return $"art: {lib.models.Count} models, {lib.clips.Count} clips";
        }
    }
}
