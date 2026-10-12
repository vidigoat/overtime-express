using System.Text;
using UnityEditor;
using UnityEngine;

namespace Overtime.EditorTools
{
    /// <summary>Imports the animation library as Humanoid so clips from both packs play on one character, looping where they should.</summary>
    public static class UalSetup
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            foreach (var f in new[] { "UAL1", "UAL2" })
            {
                string path = $"Assets/Overtime/Art/UAL/{f}.fbx";
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                var clips = imp.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.loopTime = c.name.Contains("Loop");
                    c.lockRootRotation = true; c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                    c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var an = go.GetComponent<Animator>();
                sb.Append($"{f}: avatar={(an && an.avatar ? an.avatar.isValid + "/" + an.avatar.isHuman : "none")} ");
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach (var m in smr.sharedMaterials) sb.Append("mat:" + (m ? m.name : "null") + " ");
                foreach (var b in smr.bones) if (b.name.ToLower().Contains("spine") || b.name.ToLower().Contains("head") || b.name.ToLower().Contains("pelvis") || b.name.ToLower().Contains("neck")) sb.Append(b.name + " ");
                sb.Append("| ");
            }
            return sb.ToString();
        }
    }
}
