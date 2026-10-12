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
    }
}
