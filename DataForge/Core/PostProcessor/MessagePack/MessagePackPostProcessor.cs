using Elder.DataForge.Core.Common.Const;
using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.PostProcessor.MessagePack
{
    public class MessagePackPostProcessor : IProgressNotifier
    {
        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void UpdateOutputLog(string outputLog) => _updateOutputLog.OnNext(outputLog);

        public async Task<bool> PostProcessAsync()
        {
            try
            {
                UpdateProgressValue(5f);

                // 1. MPC 환경 체크 및 설치
                UpdateProgressLevel("Checking MessagePack Generator tool...");
                var isMpcInstalled = await EnsureMpcToolInstalledAsync();
                if (!isMpcInstalled)
                {
                    UpdateOutputLog("[MPC] Failed to install or find MessagePack Generator.");
                    return false;
                }
                UpdateProgressValue(25.0f);

                // 2. 임시 .csproj 생성
                // mpc가 분석할 수 있도록 소스 코드 경로와 같은 위치 혹은 하위 폴더에 생성합니다.
                UpdateProgressLevel("Generating temporary project file for analysis...");

                string projectRoot = Properties.Settings.Default.OutputPath;
                string tempProjectDir = Path.Combine(projectRoot, "_TempMpcProject");

                string csprojPath = await GenerateCsprojFile(tempProjectDir, DataForgeConsts.AssemblyName);
                UpdateProgressValue(45.0f);

                // 3. mpc.exe 실행 (Resolver 생성)
                UpdateProgressLevel("Running MessagePack Generator (MPC)...");

                bool mpcSuccess = await RunMPCAsync(csprojPath);

                if (!mpcSuccess)
                {
                    UpdateProgressLevel("MPC Generation failed. Check debug console.");
                    return false;
                }
                UpdateProgressValue(85.0f);

                // 4. 후처리 (임시 프로젝트 폴더 삭제)
                UpdateProgressLevel("Cleaning up temporary files...");
                if (Directory.Exists(tempProjectDir))
                    Directory.Delete(tempProjectDir, true);

                UpdateProgressLevel("MessagePack Post-Processing Complete.");
                UpdateProgressValue(100.0f);

                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Error: {ex.Message}");
                UpdateOutputLog($"[ProcessAsync] Critical Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EnsureMpcToolInstalledAsync()
        {
            try
            {
                string projectRoot = Properties.Settings.Default.OutputPath;
                string manifestPath = Path.Combine(projectRoot, ".config", "dotnet-tools.json");

                // 1. 매니페스트 확인 및 생성 (없으면 dotnet new tool-manifest)
                if (!File.Exists(manifestPath))
                {
                    UpdateProgressLevel("Initializing Tool Manifest...");
                    var startInfo = new ProcessStartInfo("dotnet", "new tool-manifest")
                    {
                        WorkingDirectory = projectRoot,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(startInfo);
                    if (p != null) await p.WaitForExitAsync();
                }

                // 2. MPC 툴 로컬 설치 시도
                UpdateProgressLevel("Syncing MessagePack Generator Tool...");
                var installInfo = new ProcessStartInfo("dotnet", "tool install MessagePack.Generator")
                {
                    WorkingDirectory = projectRoot,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var pInstall = Process.Start(installInfo);
                if (pInstall != null)
                    await pInstall.WaitForExitAsync();

                return true;
            }
            catch (Exception ex)
            {
                UpdateOutputLog($"Tool Setup Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RunMPCAsync(string csprojPath)
        {
            try
            {
                string projectRoot = AppDomain.CurrentDomain.BaseDirectory;

                var restoreInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool restore",
                    WorkingDirectory = projectRoot,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var restoreProcess = Process.Start(restoreInfo))
                    await restoreProcess?.WaitForExitAsync();

                string outputPath = Properties.Settings.Default.OutputPath;
                string gameDataPath = Path.Combine(outputPath, SourceCategory.GameData.ToString());
                string resolverFolderPath = Path.Combine(outputPath, DataForgeConsts.Resolver);
                string resolverOutputPath = Path.Combine(resolverFolderPath, MessagePackConsts.ResolverFileName);
                string nameSpace = Properties.Settings.Default.RootNamespace;

                // ─── GameData 폴더가 없으면 생성 (MPC -i 필수 옵션 충족) ──────
                if (!Directory.Exists(gameDataPath))
                    Directory.CreateDirectory(gameDataPath);

                if (!Directory.Exists(resolverFolderPath))
                    Directory.CreateDirectory(resolverFolderPath);

                string msBuildPath = FindMsBuildPath();
                if (string.IsNullOrEmpty(msBuildPath))
                {
                    UpdateOutputLog("[MPC] Error: MSBuild.exe를 찾을 수 없습니다. VS 2022 설치 확인이 필요합니다.");
                    return false;
                }

                string msBuildBinDir = Path.GetDirectoryName(msBuildPath);
                string vsRoot = Path.GetFullPath(Path.Combine(msBuildBinDir, @"..\..\..\..\"));
                string sdksPath = Path.Combine(vsRoot, @"MSBuild\Sdks");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    // -i GameData(필수), -p csproj(GameData+SharedDTO 분석), -o 출력, -n 네임스페이스
                    Arguments = $"tool run mpc -i \"{gameDataPath}\" -p \"{csprojPath}\" -o \"{resolverOutputPath}\" -n \"{nameSpace}\"",
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.GetEncoding("EUC-KR"),
                    StandardOutputEncoding = Encoding.GetEncoding("EUC-KR"),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.EnvironmentVariables["MSBUILD_EXE_PATH"] = msBuildPath;
                startInfo.EnvironmentVariables["MSBuildSDKsPath"] = sdksPath;

                string dotnetDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (!string.IsNullOrEmpty(dotnetDir))
                    startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetDir;

                using (var process = Process.Start(startInfo))
                {
                    if (process == null) return false;

                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    string stdOutput = await outputTask;
                    string stdError = await errorTask;
                    
                    if (process.ExitCode != 0)
                    {
                        UpdateOutputLog($"MPC Fail Output: {stdOutput}");
                        UpdateOutputLog($"MPC Fail Error: {stdError}");
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                UpdateOutputLog($"RunMPC Critical Exception: {ex.Message}");
                return false;
            }
        }

        private string FindMsBuildPath()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.MsBuildPath))
                return Properties.Settings.Default.MsBuildPath;

            string[] searchPaths = GetPotentialMSBuildPaths();
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private string[] GetPotentialMSBuildPaths()
        {
            string relativePath = @"Program Files\Microsoft Visual Studio\2022\{0}\MSBuild\Current\Bin\MSBuild.exe";
            string[] editions = { "Community", "Professional", "Enterprise" };

            // 현재 시스템의 모든 논리 드라이브 (C:\, D:\ 등) 가져오기
            var drives = DriveInfo.GetDrives().Select(d => d.Name).ToList();

            return drives
                .SelectMany(drive => editions.Select(edition => Path.Combine(drive, string.Format(relativePath, edition))))
                .Where(File.Exists) // 실제로 파일이 존재하는 경로만 필터링
                .ToArray();
        }

        private async Task<string> GenerateCsprojFile(string targetPath, string assemblyName, string additionalTags = "")
        {
            try
            {
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string libsDir = Path.Combine(baseDir, "Libs");
                string entitiesDllPath = Path.Combine(libsDir, "Unity.Entities.dll");
                string collectionsDllPath = Path.Combine(libsDir, "Unity.Collections.dll");

                StringBuilder refBuilder = new StringBuilder();
                refBuilder.AppendLine("  <ItemGroup>");
                refBuilder.AppendLine($@"    <Reference Include=""Unity.Entities""><HintPath>{entitiesDllPath}</HintPath></Reference>");
                refBuilder.AppendLine($@"    <Reference Include=""Unity.Collections""><HintPath>{collectionsDllPath}</HintPath></Reference>");
                refBuilder.AppendLine("  </ItemGroup>");

                // ─── MPC 분석 대상: GameData + SharedDTO ──────────────────────
                string outputPath = Settings.Default.OutputPath;
                string gameDataPath = Path.Combine(outputPath, SourceCategory.GameData.ToString());
                string sharedDtoPath = Path.Combine(outputPath, SourceCategory.SharedDTO.ToString());

                var compileBuilder = new StringBuilder();
                if (Directory.Exists(gameDataPath))
                    compileBuilder.AppendLine($@"    <Compile Include=""{gameDataPath}\**\*.cs"" />");
                if (Directory.Exists(sharedDtoPath))
                    compileBuilder.AppendLine($@"    <Compile Include=""{sharedDtoPath}\**\*.cs"" />");

                // additionalTags와 합산
                string fullAdditionalTags = compileBuilder.ToString() + additionalTags;

                string content = string.Format(MessagePackConsts.DodProjectTemplate, assemblyName, fullAdditionalTags, refBuilder.ToString());
                string filePath = Path.Combine(targetPath, $"{assemblyName}.csproj");

                await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(false));
                return filePath;
            }
            catch (Exception ex)
            {
                UpdateOutputLog($"[CsprojGen] Error: {ex.Message}");
                throw;
            }
        }
    }
}