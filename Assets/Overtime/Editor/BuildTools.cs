using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Overtime.EditorTools
{
    /// <summary>Builds the browser version into Builds/Web, ready to deploy as a static site.</summary>
    public static class BuildTools
    {
        [MenuItem("Overtime/Build Web")]
        public static string BuildWeb()
        {
            PlayerSettings.WebGL.template = "PROJECT:Overtime";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;   // the host compresses static files itself
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Web", target = BuildTarget.WebGL, options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            return $"{s.result}: {s.totalSize / 1048576f:F1} MB in {s.totalTime.TotalSeconds:F0}s, {s.totalErrors} errors";
        }

        /// <summary>
        /// Exports the native iPhone project into Builds/iOS: portrait, full screen, 120 Hz, automatic signing.
        /// Xcode then signs it with the team from the Apple ID signed into Xcode and installs it on a plugged-in phone.
        /// </summary>
        /// <summary>The gold Tock over the neon road, scaled by Unity to every size the platforms need.</summary>
        public static string SetAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Overtime/Art/AppIcon.png");
            if (icon == null) return "no icon";
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            return "icon set";
        }

        /// <summary>
        /// The store listing says the app collects no data and makes no network calls, so the player must not send
        /// Unity's hardware statistics or crash reports. submitAnalytics has no public setter, hence the serialized edit.
        /// </summary>
        public static string DisableTelemetry()
        {
            PlayerSettings.enableCrashReportAPI = false;
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            var so = new SerializedObject(asset);
            so.FindProperty("submitAnalytics").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return "telemetry off";
        }

        [MenuItem("Overtime/Build iPhone")]
        public static string BuildIOS() => BuildIOS(PlayerSettings.iOS.appleDeveloperTeamID);
        public static string BuildIOS(string team)
        {
            PlayerSettings.companyName = "Overtime Express"; PlayerSettings.productName = "Overtime";
            SetAppIcon();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.vidigoat.overtime");
            PlayerSettings.bundleVersion = "1.0"; PlayerSettings.iOS.buildNumber = System.DateTime.Now.ToString("yyMMddHHmm");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.iOS.targetOSVersionString = "16.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;      // no iPad layout, so no iPad screenshots needed
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
            PlayerSettings.iOS.hideHomeButton = false; PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.BottomEdge;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.iOS, 1);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            DisableTelemetry();
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/iOS", target = BuildTarget.iOS, options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var r = report.summary;
            return $"{r.result}: {r.totalTime.TotalSeconds:F0}s, {r.totalErrors} errors";
        }
    }
}
