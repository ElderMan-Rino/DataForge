using Elder.DataForge.Core.Common.Const.MemoryPack;
using Elder.DataForge.Core.Interfaces;
using OfficeOpenXml.Utils;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Text;


namespace Elder.DataForge.Core.PostProcessor.MessagePack
{
    public class MessagePackPostProcessor : IProgressNotifier
    {
        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);


        public async Task<bool> PostProcessAsync(PostProcessContext context)
        {
            try
            {
                UpdateProgressValue(0.1f);

                // 1. MPC 환경 체크 및 설치
                UpdateProgressLevel("Checking MessagePack Generator tool...");
                var isMpcInstalled = await EnsureMpcToolInstalledAsync();
                if (!isMpcInstalled)
                {
                    Debug.WriteLine("[MPC] Failed to install or find MessagePack Generator.");
                    return false;
                }
                UpdateProgressValue(0.3f);

                // 2. 임시 .csproj 생성
                // mpc가 분석할 수 있도록 소스 코드 경로와 같은 위치 혹은 하위 폴더에 생성합니다.
                UpdateProgressLevel("Generating temporary project file for analysis...");
                
                string projectRoot = Properties.Settings.Default.OutputPath;
                string tempProjectDir = Path.Combine(projectRoot, "_TempMpcProject");

                string csprojPath = await GenerateCsprojFile(tempProjectDir, MemoryPackConsts.AssemblyName);
                UpdateProgressValue(0.5f);

                // 3. mpc.exe 실행 (Resolver 생성)
                UpdateProgressLevel("Running MessagePack Generator (MPC)...");

                bool mpcSuccess = RunMPC(csprojPath);

                if (!mpcSuccess)
                {
                    UpdateProgressLevel("MPC Generation failed. Check debug console.");
                    return false;
                }
                UpdateProgressValue(0.8f);

                // 4. 후처리 (임시 프로젝트 폴더 삭제)
                UpdateProgressLevel("Cleaning up temporary files...");
                if (Directory.Exists(tempProjectDir))
                    Directory.Delete(tempProjectDir, true);

                UpdateProgressLevel("MessagePack Post-Processing Complete.");
                UpdateProgressValue(1.0f);

                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Error: {ex.Message}");
                Debug.WriteLine($"[ProcessAsync] Critical Error: {ex}");
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
                Debug.WriteLine($"Tool Setup Error: {ex.Message}");
                return false;
            }
        }

        private bool RunMPC(string csprojPath)
        {
            try
            {
                string inputPath = Path.Combine(Properties.Settings.Default.OutputPath, MemoryPackConsts.DODSuffix);
                string resolverFolderPath = Path.Combine(Properties.Settings.Default.OutputPath, MemoryPackConsts.Resolver);
                string outputPath = Path.Combine(resolverFolderPath, MemoryPackConsts.ResolverFileName);
                string nameSpace = Properties.Settings.Default.RootNamespace;
                // 1. MSBuild 실제 경로 탐색
                string projectRoot = Properties.Settings.Default.OutputPath;
                string msBuildPath = FindMsBuildPath();
                if (string.IsNullOrEmpty(msBuildPath))
                {
                    Debug.WriteLine("[MPC] Error: MSBuild.exe를 찾을 수 없습니다. VS 2022 설치 확인이 필요합니다.");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments =  $"tool run mpc -p \"{csprojPath}\" -o \"{outputPath}\" -n \"{nameSpace}\"",
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.GetEncoding("EUC-KR"),
                    StandardOutputEncoding = Encoding.GetEncoding("EUC-KR"),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 2. 환경 변수 강제 주입 (D드라이브 설치 시 필수)
                string msBuildBinDir = Path.GetDirectoryName(msBuildPath);
                string vsRoot = Path.GetFullPath(Path.Combine(msBuildBinDir, @"..\..\..\..\")); // MSBuild\Current\Bin -> MSBuild 기준
                string sdksPath = Path.Combine(vsRoot, @"MSBuild\Sdks");

                startInfo.EnvironmentVariables["MSBUILD_EXE_PATH"] = msBuildPath;
                startInfo.EnvironmentVariables["MSBuildSDKsPath"] = sdksPath; // 이게 없으면 D드라이브에서 SDK 참조를 못합니다.

                // .NET 9 환경 변수 보정
                string dotnetDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (!string.IsNullOrEmpty(dotnetDir))
                {
                    startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetDir;
                }

                using (var process = Process.Start(startInfo))
                {
                    if (process == null) return false;

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.WriteLine($"MPC Fail Output: {output}");
                        Debug.WriteLine($"MPC Fail Error: {error}");
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RunMPC Critical Exception: {ex.Message}");
                return false;
            }
        }

        private string FindMsBuildPath()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.MsBuildPath))
                return Properties.Settings.Default.MsBuildPath;

            // Properties.Settings.Default.MsBuildPath가 없을 경우 임의로 
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

                string content = string.Format(MemoryPackConsts.DodProjectTemplate, assemblyName, additionalTags);
                string filePath = Path.Combine(targetPath, $"{assemblyName}.csproj");

                // UTF8 with BOM은 가끔 외부 툴에서 문제를 일으키므로, 인코딩 선택에 유의하세요.
                await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(false));

                return filePath; // 경로를 반환하여 다음 프로세스(RunMPC)에서 바로 쓰게 함
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CsprojGen] Error: {ex.Message}");
                throw; // 상위 ProcessAsync에서 에러 처리를 하도록 던짐
            }
        }
    }
}
