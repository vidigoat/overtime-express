#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Overtime.EditorTools
{
    /// <summary>App Store housekeeping on the exported Xcode project: no-encryption declaration, iPhone only, full screen, 120 Hz.</summary>
    public static class IOSPostBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument(); plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            root.SetBoolean("UIRequiresFullScreen", true);
            root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);
            root.SetString("CFBundleDisplayName", "Overtime");
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
