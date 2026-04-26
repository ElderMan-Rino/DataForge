using Elder.DataForge.Core.Common.Const;
using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.DllBuilder
{
    public class DllBuilder : IDllBuilder
    {
        private readonly Subject<string> _updateProgressLevel = new();
        private readonly Subject<string> _updateOutputLog = new();
        private readonly Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void LogMessage(string message) => _updateOutputLog.OnNext(message);

        public async Task<bool> BuildDllAsync()
        {
            try
            {
                UpdateProgressValue(0f);
                UpdateProgressLevel("Preparing DLL Build...");

                if (string.IsNullOrEmpty(Settings.Default.OutputPath) || string.IsNullOrEmpty(Settings.Default.OutputDllName))
                {
                    UpdateProgressLevel("Build Stopped: Output Path or Name is not configured.");
                    return false;
                }

                UpdateProgressValue(10f);

                string rootOutputPath = Settings.Default.OutputPath;
                string gameDataPath = Path.Combine(rootOutputPath, SourceCategory.GameData.ToString());
                string enumsPath = Path.Combine(rootOutputPath, SourceCategory.Enums.ToString());
                string resolverPath = Path.Combine(rootOutputPath, DataForgeConsts.Resolver);
                string sourceFolderPath = Path.Combine(rootOutputPath, "_TempMpcProject");
                string dllsDirectory = Path.Combine(rootOutputPath, "Dlls");
                string outputDllPath = Path.Combine(dllsDirectory, Settings.Default.OutputDllName + ".dll");

                if (!Directory.Exists(sourceFolderPath)) Directory.CreateDirectory(sourceFolderPath);
                if (!Directory.Exists(dllsDirectory)) Directory.CreateDirectory(dllsDirectory);

                UpdateProgressValue(20f);

                // ✨ 1. 로컬 Libs 절대 경로 및 필수 DLL 목록 정의
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string libsDir = Path.Combine(baseDir, "Libs");

                // 빌드에 필요한 모든 외부 라이브러리 목록
                string[] requiredDlls = {
                    "Unity.Entities.dll",
                    "Unity.Collections.dll",
                    "Unity.Mathematics.dll",
                    "MessagePack.dll",
                    "UniTask.dll", // Cysharp.Threading.Tasks 용
                    "Elder.Framework.Data.Interfaces.dll" // IGameDataLoader 등 프레임워크 인터페이스 용
                };

                StringBuilder refBuilder = new StringBuilder();
                refBuilder.AppendLine("  <ItemGroup>");

                foreach (var dllName in requiredDlls)
                {
                    string fullPath = Path.Combine(libsDir, dllName);
                    if (!File.Exists(fullPath))
                    {
                        LogMessage($"[Warning] 필수 참조 누락: {dllName} 파일이 Libs 폴더에 없습니다. ({fullPath})");
                        continue;
                    }

                    string assemblyName = Path.GetFileNameWithoutExtension(dllName);
                    refBuilder.AppendLine($@"    <Reference Include=""{assemblyName}""><HintPath>{fullPath}</HintPath></Reference>");
                }
                refBuilder.AppendLine("  </ItemGroup>");

                UpdateProgressValue(30f);

                // 2. .csproj 생성
                string enumsCompile = Directory.Exists(enumsPath)
                    ? $"\n    <Compile Include=\"{enumsPath}\\**\\*.cs\" />"
                    : string.Empty;

                string compileItems = $@"
    <Compile Include=""{gameDataPath}\**\*.cs"" />{enumsCompile}
    <Compile Include=""{resolverPath}\**\*.cs"" />
";

                string assemblyNameResult = Path.GetFileNameWithoutExtension(outputDllPath);
                string csprojContent = string.Format(MessagePackConsts.DodProjectTemplate, assemblyNameResult, compileItems, refBuilder.ToString());

                string csprojPath = Path.Combine(sourceFolderPath, $"{assemblyNameResult}.csproj");
                await File.WriteAllTextAsync(csprojPath, csprojContent, Encoding.UTF8);

                // 3. nuget.config 생성
                string nugetConfigContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
                await File.WriteAllTextAsync(Path.Combine(sourceFolderPath, "nuget.config"), nugetConfigContent, Encoding.UTF8);

                UpdateProgressValue(50f);

                // 4. dotnet build 실행
                UpdateProgressLevel($"Compiling DLL: {assemblyNameResult}...");
                UpdateProgressValue(70f);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{csprojPath}\" -c Release -o \"{Path.GetDirectoryName(outputDllPath)}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null) throw new Exception("dotnet 프로세스를 시작할 수 없습니다.");

                    string outputLogs = await process.StandardOutput.ReadToEndAsync();
                    string errorLogs = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        StringBuilder errorBuilder = new StringBuilder();
                        errorBuilder.AppendLine("=== Build Failed ===");
                        if (!string.IsNullOrWhiteSpace(errorLogs)) errorBuilder.AppendLine($"[StdErr]\n{errorLogs}");
                        if (!string.IsNullOrWhiteSpace(outputLogs)) errorBuilder.AppendLine($"[StdOut]\n{outputLogs}");

                        LogMessage(errorBuilder.ToString());
                        UpdateProgressLevel("Build Failed! 출력 로그를 확인하세요.");
                        return false;
                    }
                }

                UpdateProgressLevel("DLL Build Success!");
                LogMessage($"[{DateTime.Now:HH:mm:ss}] DLL 생성 성공: {outputDllPath}");
                UpdateProgressValue(100f);
                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Build Exception: {ex.Message}");
                LogMessage($"[Exception] {ex.Message}\n{ex.StackTrace}");
                UpdateProgressValue(0f);
                return false;
            }
        }
    }
}