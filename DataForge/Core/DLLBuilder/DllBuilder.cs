using Elder.DataForge.Core.Common.Const;
using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Text;

namespace Elder.DataForge.Core.DllBuilder
{
    public class DllBuilder : IDllBuilder
    {
        private readonly Subject<string> _updateProgressLevel = new();
        private readonly Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public async Task<bool> BuildDllAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.OutputPath))
                {
                    UpdateProgressLevel("Build Stopped: **Base Output Path** is not configured.");
                    return false;
                }

                if (string.IsNullOrEmpty(Properties.Settings.Default.OutputDllName))
                {
                    UpdateProgressLevel("Build Stopped: **Output DLL Name** is not configured.");
                    return false;
                }

                // 1. 경로 계산 (Settings 기반)
                string rootOutputPath = Settings.Default.OutputPath;

                // 소스 폴더 (DOD, Resolvers)
                string dodPath = Path.Combine(rootOutputPath, MessagePackConsts.DODSuffix);
                string resolverPath = Path.Combine(rootOutputPath, DataForgeConsts.Resolver);

                // 임시 프로젝트 폴더 및 출력 폴더
                string sourceFolderPath = Path.Combine(rootOutputPath, "_TempMpcProject");
                string dllsDirectory = Path.Combine(rootOutputPath, "Dlls");
                string outputDllPath = Path.Combine(dllsDirectory, Settings.Default.OutputDllName + ".dll");

                // 폴더 생성 보장
                if (!Directory.Exists(sourceFolderPath))
                    Directory.CreateDirectory(sourceFolderPath);

                if (!Directory.Exists(dllsDirectory)) 
                    Directory.CreateDirectory(dllsDirectory);

                string assemblyName = Path.GetFileNameWithoutExtension(outputDllPath);
                string csprojContent = string.Format(MessagePackConsts.DodProjectTemplate, assemblyName);

                string csprojPath = Path.Combine(sourceFolderPath, $"{assemblyName}.csproj");
                await File.WriteAllTextAsync(csprojPath, csprojContent, Encoding.UTF8);

                // 2. dotnet build 실행
                UpdateProgressLevel($"Compiling DLL: {assemblyName}...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    // -s 옵션을 사용하여 기본 NuGet 저장소와 UnityNuGet 저장소를 모두 지정합니다.
                    Arguments = $"build \"{csprojPath}\" -c Release -o \"{Path.GetDirectoryName(outputDllPath)}\" " + $"-s \"https://api.nuget.org/v3/index.json\" " +$"-s \"https://unitynuget-registry.openupm.com\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        UpdateProgressLevel($"Build Error: {error}");
                        return false;
                    }
                }

                UpdateProgressLevel("DLL Build Success!");
                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Build Failed: {ex.Message}");
                return false;
            }
        }
    }
}