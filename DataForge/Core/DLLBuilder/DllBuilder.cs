using Elder.DataForge.Core.Common.Const;
using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System;
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

                // 설정 검증
                if (string.IsNullOrEmpty(Settings.Default.OutputPath) || string.IsNullOrEmpty(Settings.Default.OutputDllName))
                {
                    UpdateProgressLevel("Build Stopped: Output Path or Name is not configured.");
                    return false;
                }

                UpdateProgressValue(10f);

                // 1. 경로 계산
                string rootOutputPath = Settings.Default.OutputPath;
                string gameDataPath = Path.Combine(rootOutputPath, SourceCategory.GameData.ToString());
                string resolverPath = Path.Combine(rootOutputPath, DataForgeConsts.Resolver);
                string sourceFolderPath = Path.Combine(rootOutputPath, "_TempMpcProject");
                string dllsDirectory = Path.Combine(rootOutputPath, "Dlls");
                string outputDllPath = Path.Combine(dllsDirectory, Settings.Default.OutputDllName + ".dll");

                if (!Directory.Exists(sourceFolderPath)) Directory.CreateDirectory(sourceFolderPath);
                if (!Directory.Exists(dllsDirectory)) Directory.CreateDirectory(dllsDirectory);

                UpdateProgressValue(20f);

                // ✨ 2. 로컬 Libs 절대 경로 계산 및 파일 검증
                // AppDomain.CurrentDomain.BaseDirectory는 현재 실행 중인 .exe의 폴더 위치를 반환합니다.
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string libsDir = Path.Combine(baseDir, "Libs");

                string entitiesDllPath = Path.Combine(libsDir, "Unity.Entities.dll");
                string collectionsDllPath = Path.Combine(libsDir, "Unity.Collections.dll");

                // 빌드 전 파일이 실제로 존재하는지 체크하여 에러를 방지합니다.
                if (!File.Exists(entitiesDllPath) || !File.Exists(collectionsDllPath))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Critical Error: Required Unity DLLs not found in Libs folder!");
                    sb.AppendLine($"Expected Path: {libsDir}");
                    LogMessage(sb.ToString());
                    UpdateProgressLevel("Build Failed: Missing local DLLs.");
                    return false;
                }

                // ✨ 3. .csproj에 주입할 참조 구문 생성 (절대 경로 HintPath 사용)
                // HintPath를 통해 MSBuild가 프로젝트 외부의 DLL을 정확히 찾도록 합니다.
                StringBuilder refBuilder = new StringBuilder();
                refBuilder.AppendLine("  <ItemGroup>");
                refBuilder.AppendLine($@"    <Reference Include=""Unity.Entities""><HintPath>{entitiesDllPath}</HintPath></Reference>");
                refBuilder.AppendLine($@"    <Reference Include=""Unity.Collections""><HintPath>{collectionsDllPath}</HintPath></Reference>");
                refBuilder.AppendLine("  </ItemGroup>");

                UpdateProgressValue(30f);

                // 4. csproj 파일 생성
                string compileItems = $@"
    <Compile Include=""{gameDataPath}\**\*.cs"" Exclude=""{gameDataPath}\GeneratedDataLoader.cs"" />
    <Compile Include=""{resolverPath}\**\*.cs"" />
";
                string assemblyName = Path.GetFileNameWithoutExtension(outputDllPath);

                // MessagePackConsts.DodProjectTemplate의 {2} 위치에 refBuilder(참조 구문)를 넣습니다.
                string csprojContent = string.Format(MessagePackConsts.DodProjectTemplate, assemblyName, compileItems, refBuilder.ToString());

                string csprojPath = Path.Combine(sourceFolderPath, $"{assemblyName}.csproj");
                await File.WriteAllTextAsync(csprojPath, csprojContent, Encoding.UTF8);

                // 5. nuget.config 생성 (오프라인 빌드 안정성을 위해 nuget.org만 남김)
                string nugetConfigContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
                string nugetConfigPath = Path.Combine(sourceFolderPath, "nuget.config");
                await File.WriteAllTextAsync(nugetConfigPath, nugetConfigContent, Encoding.UTF8);

                UpdateProgressValue(50f);

                // 6. dotnet build 실행
                UpdateProgressLevel($"Compiling DLL: {assemblyName}...");
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
                startInfo.EnvironmentVariables["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
                startInfo.EnvironmentVariables["VSLANG"] = "1033";

                using (var process = Process.Start(startInfo))
                {
                    if (process == null) throw new Exception("Failed to start dotnet process.");

                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    string outputLogs = await outputTask;
                    string errorLogs = await errorTask;

                    if (process.ExitCode != 0)
                    {
                        StringBuilder errorBuilder = new StringBuilder();
                        errorBuilder.AppendLine("=== Build Failed ===");
                        if (!string.IsNullOrWhiteSpace(errorLogs)) errorBuilder.AppendLine($"[StdErr]\n{errorLogs}");
                        if (!string.IsNullOrWhiteSpace(outputLogs)) errorBuilder.AppendLine($"[StdOut]\n{outputLogs}");

                        string fullError = errorBuilder.ToString();
                        string errorLogFilePath = Path.Combine(dllsDirectory, "build_error.log");
                        await File.WriteAllTextAsync(errorLogFilePath, fullError, Encoding.UTF8);

                        UpdateProgressLevel("Build Failed! Check Output Logs.");
                        LogMessage(fullError);
                        UpdateProgressValue(0f);
                        return false;
                    }
                }

                UpdateProgressLevel("DLL Build Success!");
                LogMessage($"[{DateTime.Now:HH:mm:ss}] DLL successfully built at: {outputDllPath}");
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