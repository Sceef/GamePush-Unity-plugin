#if UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GamePushEditor.Play2Web
{
    // Builds the standalone WebView2 overlay host with the system C# compiler. Shipping a
    // prebuilt exe would mean a binary in version control, so the host is compiled on demand
    // and cached in Temp next to its WebView2 dependencies.
    static class GP_Play2WebHostBuilder
    {
        const string SourceAsset = "Assets/Plugins/GamePush/Editor/Play2Web/gp-play2web-hostapp.cs.txt";
        const string CoreDll = "Microsoft.Web.WebView2.Core.dll";
        const string LoaderDll = "WebView2Loader.dll";

        static string HostDir => Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? "",
            "Temp", "GamePushPlay2Web", "host");

        static string ExePath => Path.Combine(HostDir, "gp-play2web-host.exe");

        public static string EnsureBuilt()
        {
            var source = Path.GetFullPath(SourceAsset);
            if (!File.Exists(source))
            {
                UnityEngine.Debug.LogError("[Play2Web] Overlay host source missing: " + SourceAsset);
                return null;
            }

            var pluginDir = Path.GetFullPath(Path.Combine(
                Application.dataPath, "Plugins", "GamePush", "Editor", "WebView2"));
            var core = Path.Combine(pluginDir, CoreDll);
            var loader = Path.Combine(pluginDir, LoaderDll);
            if (!File.Exists(core) || !File.Exists(loader))
            {
                UnityEngine.Debug.LogError("[Play2Web] WebView2 assemblies missing in " + pluginDir);
                return null;
            }

            Directory.CreateDirectory(HostDir);
            File.Copy(core, Path.Combine(HostDir, CoreDll), true);
            File.Copy(loader, Path.Combine(HostDir, LoaderDll), true);

            if (File.Exists(ExePath) && File.GetLastWriteTimeUtc(ExePath) > File.GetLastWriteTimeUtc(source))
                return ExePath;

            return Compile(source);
        }

        static string Compile(string source)
        {
            var csc = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");
            if (!File.Exists(csc))
            {
                UnityEngine.Debug.LogError("[Play2Web] .NET Framework C# compiler not found at " + csc);
                return null;
            }

            var sourceCopy = Path.Combine(HostDir, "gp-play2web-hostapp.cs");
            File.Copy(source, sourceCopy, true);

            var arguments =
                "/nologo /target:winexe /optimize+ /platform:x64 " +
                $"/out:\"{ExePath}\" " +
                "/reference:System.dll /reference:System.Core.dll " +
                "/reference:System.Drawing.dll /reference:System.Windows.Forms.dll " +
                $"/reference:\"{Path.Combine(HostDir, CoreDll)}\" " +
                $"\"{sourceCopy}\"";

            var info = new ProcessStartInfo
            {
                FileName = csc,
                Arguments = arguments,
                WorkingDirectory = HostDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(info))
            {
                if (process == null)
                {
                    UnityEngine.Debug.LogError("[Play2Web] Could not run the C# compiler.");
                    return null;
                }

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(60000);

                if (process.ExitCode != 0 || !File.Exists(ExePath))
                {
                    UnityEngine.Debug.LogError("[Play2Web] Overlay host build failed:\n" + stdout + stderr);
                    return null;
                }
            }

            GP_Play2WebWindow.PushLog("Overlay host compiled");
            return ExePath;
        }
    }
}
#endif
