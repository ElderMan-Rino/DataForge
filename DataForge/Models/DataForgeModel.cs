using DataForge.DataForge.Core.SchemaAnalyzer;
using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Core.CodeSaver;
using Elder.DataForge.Core.ContentLoaders.Excels;
using Elder.DataForge.Core.InfoLoaders.Excels;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.Reactives.Helpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text; // Encoding 설정을 위해 필요
using System.Windows;
using Elder.DataForge.Properties;

namespace Elder.DataForge.Models
{
    internal class DataForgeModel : IModel
    {
        private IDocumentInfoLoader _infoLoader = new ExcelInfoLoader();
        private IDocumentContentExtracter _contentExtracter = new ExcelContentExtracter();

        private CompositeDisposable _disposables = new();
       
        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        
        
        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; private set; } = new();
        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        
        public DataForgeModel()
        {
            SubscribeToContentLoader();
        }

        private void SubscribeToContentLoader()
        {
            _contentExtracter.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
            _contentExtracter.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
        }

        private void OnSourceProgressLevelUpdated(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => _updateProgressValue.OnNext(progressValue);



        public void Handle

        //        private IDocumentInfoLoader _infoLoader = new ExcelInfoLoader();
        //        private IDocumentContentExtracter _contentExtracter = new ExcelContentExtracter();
        //        private ISourceCodeGenerator _codeGenerator = new MessagePackSourceGenerator();
        //        private ISourceCodeSaver _codeSaver = new FileSourceCodeSaver();
        //        private ITableSchemaAnalyzer _schemaAnalyzer = new TableSchemaAnalyzer();

        //        // [경로 설정] 프로젝트 루트와 출력 경로 정의
        //        private string _projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
        //        private string _baseOutputPath;
        //        private string _rootNamespace;

        //        private const string DodProjectTemplate =
        //@"<Project Sdk=""Microsoft.NET.Sdk"">
        //  <PropertyGroup>
        //    <TargetFramework>netstandard2.1</TargetFramework>
        //    <ImplicitUsings>enable</ImplicitUsings>
        //    <Nullable>enable</Nullable>
        //    <AssemblyName>{0}</AssemblyName>
        //  </PropertyGroup>
        //  <ItemGroup>
        //    <PackageReference Include=""MessagePack"" Version=""2.5.140"" />
        //  </ItemGroup>
        //</Project>";

        //        private Dictionary<string, DocumentInfoData> _documenttInfoDataMap = new();
        //        private Dictionary<string, DocumentContentData> _documentContents = new();

        //        private CompositeDisposable _disposables = new();
        //        private Subject<string> _updateProgressLevel = new();
        //        private Subject<float> _updateProgressValue = new();

        //        private bool _disposed = false;
        //        private bool _tasking = false;


        //        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        //        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        //        public DataForgeModel()
        //        {
        //            LoadSettingsValue();
        //            SubscribeToContentLoader();
        //        }

        //        /// <summary>
        //        /// MPC 로컬 도구가 설치되어 있는지 확인하고 없으면 자동으로 설치합니다.
        //        /// </summary>
        //        private async Task<bool> EnsureMpcToolInstalled()
        //        {
        //            try
        //            {
        //                string manifestPath = Path.Combine(_projectRoot, ".config", "dotnet-tools.json");

        //                // 1. 매니페스트 확인 및 생성 (없으면 dotnet new tool-manifest)
        //                if (!File.Exists(manifestPath))
        //                {
        //                    UpdateProgressLevel("Initializing Tool Manifest...");
        //                    var startInfo = new ProcessStartInfo("dotnet", "new tool-manifest")
        //                    {
        //                        WorkingDirectory = _projectRoot,
        //                        CreateNoWindow = true,
        //                        UseShellExecute = false
        //                    };
        //                    using var p = Process.Start(startInfo);
        //                    if (p != null) await p.WaitForExitAsync();
        //                }

        //                // 2. MPC 툴 로컬 설치 시도
        //                UpdateProgressLevel("Syncing MessagePack Generator Tool...");
        //                var installInfo = new ProcessStartInfo("dotnet", "tool install MessagePack.Generator")
        //                {
        //                    WorkingDirectory = _projectRoot,
        //                    CreateNoWindow = true,
        //                    UseShellExecute = false
        //                };
        //                using var pInstall = Process.Start(installInfo);
        //                if (pInstall != null) await pInstall.WaitForExitAsync();

        //                return true;
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"Tool Setup Error: {ex.Message}");
        //                return false;
        //            }
        //        }
        //        #endregion

        //        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        //        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        //       

        //        private void SubscribeToContentLoader()
        //        {
        //            _contentExtracter.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
        //            _contentExtracter.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
        //        }

        //        public void HandleLoadDocument()
        //        {
        //            ClearDocumentInfos();
        //            ClearDocumentInfoCollection();
        //            ClearDocumentInfoDataMap();
        //            LoadDocumentInfos();
        //        }

        //        private void LoadDocumentInfos()
        //        {
        //            if (!_infoLoader.TryLoadDocumentInfos(out var documentInfoData)) return;
        //            if (documentInfoData == null || !documentInfoData.Any()) return;

        //            foreach (var datum in documentInfoData)
        //            {
        //                _documenttInfoDataMap.Add(datum.Name, datum);
        //                DocumenttInfoDataCollection.Add(datum);
        //            }
        //        }

        //        public void CreateElements() => RunTask(CreateElementsAsync);

        //        private async Task<bool> ExtractDocumentContentsAsync()
        //        {
        //            _documentContents.Clear();
        //            var documentContents = await _contentExtracter.ExtractDocumentContentDataAsync(_documenttInfoDataMap.Values);
        //            if (documentContents == null || !documentContents.Any()) return false;

        //            foreach (var content in documentContents)
        //                _documentContents.Add(content.Key, content.Value);

        //            return true;
        //        }

        //        // [핵심] 리팩토링된 전체 파이프라인
        //        private async Task<bool> CreateElementsAsync()
        //        {
        //            // 1. 환경 체크 및 도구 설치 자동화
        //            UpdateProgressLevel("Checking Environment...");
        //            await EnsureMpcToolInstalled();

        //            // 2. 원시 데이터 추출 (Extraction)
        //            UpdateProgressLevel("Extracting Document Contents...");
        //            if (!await ExtractDocumentContentsAsync()) return false;

        //            // 3. 스키마 분석 (Analysis)
        //            // 여러 엑셀 파일과 시트를 통합 분석하여 표준화된 설계도(Schemas)를 생성합니다.
        //            UpdateProgressLevel("Analyzing Table Schemas (Multi-Sheet Support)...");
        //            var schemas = _schemaAnalyzer.AnalyzeFields(_documentContents);
        //            if (schemas == null || !schemas.Any())
        //            {
        //                UpdateProgressLevel("Error: No valid sheet schemas found.");
        //                return false;
        //            }

        //            // 4. 소스 코드 생성 (Generation)
        //            // 제너레이터는 분석된 'schemas' 설계도를 보고 코드를 짭니다.
        //            UpdateProgressLevel("Generating Source Codes from Schemas...");
        //            var generatedFiles = await _codeGenerator.GenerateAsync(schemas);
        //            if (generatedFiles == null || !generatedFiles.Any()) return false;

        //            // 5. 경로 설정 및 파일 분류 저장 (정규화 포함)
        //            string dataPath = Path.Combine(_baseOutputPath, "Data");
        //            string parserPath = Path.Combine(_baseOutputPath, "Parser");

        //            var dataFiles = generatedFiles.Where(f => !f.FileName.EndsWith(".Parser.cs")).ToList();
        //            var parserFiles = generatedFiles.Where(f => f.FileName.EndsWith(".Parser.cs")).ToList();

        //            UpdateProgressLevel("Saving Normalized Files...");
        //            bool s1 = await _codeSaver.SaveAsync(dataFiles, dataPath);
        //            bool s2 = await _codeSaver.SaveAsync(parserFiles, parserPath);

        //            if (!s1 || !s2) return false;

        //            // [추가] 프로젝트 파일(.csproj) 자동 생성
        //            UpdateProgressLevel("Generating Project Templates...");
        //            await GenerateCsprojFile(dataPath, "Elder.Data.DOD");

        //            // 6. MPC 실행 및 리졸버 생성
        //            UpdateProgressLevel("Running MessagePack Generator (MPC)...");
        //            string mpcPath = Path.Combine(_baseOutputPath, "Mpc");
        //            if (!Directory.Exists(mpcPath)) 
        //                Directory.CreateDirectory(mpcPath);

        //            // [해결] MSBuild 감지 실패 에러를 방지하는 RunMPC 실행
        //            bool mpcResult = await Task.Run(() => RunMPC(dataPath, mpcPath, "Elder.Framework.MessagePack.Generated"));

        //            if (mpcResult) UpdateProgressLevel("Framework Code Generation Completed.");
        //            else UpdateProgressLevel("MPC Compilation Failed. Please check console logs.");

        //            return mpcResult;
        //        }

        //        #region [Internal Logic: Project File Generation]

        //        // .csproj 파일을 생성하는 핵심 메서드
        //        private async Task GenerateCsprojFile(string targetPath, string assemblyName, string additionalTags = "")
        //        {
        //            if (!Directory.Exists(targetPath)) 
        //                Directory.CreateDirectory(targetPath);

        //            string content = string.Format(DodProjectTemplate, assemblyName, additionalTags);
        //            string filePath = Path.Combine(targetPath, $"{assemblyName}.csproj");

        //            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        //        }

        //        #endregion

        //        private bool RunMPC(string inputPath, string outputPath, string nameSpace)
        //        {
        //            try
        //            {
        //                // 1. MSBuild 실제 경로 탐색
        //                string msBuildPath = FindMsBuildPath();
        //                if (string.IsNullOrEmpty(msBuildPath))
        //                {
        //                    Debug.WriteLine("[MPC] Error: MSBuild.exe를 찾을 수 없습니다. VS 2022 설치 확인이 필요합니다.");
        //                    return false;
        //                }

        //                var startInfo = new ProcessStartInfo
        //                {
        //                    FileName = "dotnet",
        //                    Arguments = $"tool run mpc -i \"{inputPath}\" -o \"{outputPath}\" -n \"{nameSpace}\"",
        //                    WorkingDirectory = _projectRoot,
        //                    RedirectStandardOutput = true,
        //                    RedirectStandardError = true,
        //                    StandardErrorEncoding = Encoding.GetEncoding("EUC-KR"),
        //                    StandardOutputEncoding = Encoding.GetEncoding("EUC-KR"),
        //                    UseShellExecute = false,
        //                    CreateNoWindow = true
        //                };

        //                // 2. 환경 변수 강제 주입 (D드라이브 설치 시 필수)
        //                string msBuildBinDir = Path.GetDirectoryName(msBuildPath);
        //                string vsRoot = Path.GetFullPath(Path.Combine(msBuildBinDir, @"..\..\..\..\")); // MSBuild\Current\Bin -> MSBuild 기준
        //                string sdksPath = Path.Combine(vsRoot, @"MSBuild\Sdks");

        //                startInfo.EnvironmentVariables["MSBUILD_EXE_PATH"] = msBuildPath;
        //                startInfo.EnvironmentVariables["MSBuildSDKsPath"] = sdksPath; // 이게 없으면 D드라이브에서 SDK 참조를 못합니다.

        //                // .NET 9 환경 변수 보정
        //                string dotnetDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
        //                if (!string.IsNullOrEmpty(dotnetDir))
        //                {
        //                    startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetDir;
        //                }

        //                using (var process = Process.Start(startInfo))
        //                {
        //                    if (process == null) return false;

        //                    string output = process.StandardOutput.ReadToEnd();
        //                    string error = process.StandardError.ReadToEnd();
        //                    process.WaitForExit();

        //                    if (process.ExitCode != 0)
        //                    {
        //                        Debug.WriteLine($"MPC Fail Output: {output}");
        //                        Debug.WriteLine($"MPC Fail Error: {error}");
        //                        return false;
        //                    }
        //                    return true;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"RunMPC Critical Exception: {ex.Message}");
        //                return false;
        //            }
        //        }

        //        private string FindMsBuildPath()
        //        {
        //            // 사용자의 환경에 맞는 MSBuild 경로 우선 순위 설정
        //            string[] searchPaths = {
        //                @"D:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        //                @"D:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        //                @"D:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        //                @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
        //            };

        //            foreach (var path in searchPaths)
        //            {
        //                if (File.Exists(path)) return path;
        //            }
        //            return null;
        //        }

        //        public void BuildDlls() => RunTask(BuildDllsAsync);

        //        private async Task<bool> IsDotNetSdk9Installed()
        //        {
        //            try
        //            {
        //                var startInfo = new ProcessStartInfo("dotnet", "--version")
        //                {
        //                    CreateNoWindow = true,
        //                    UseShellExecute = false,
        //                    RedirectStandardOutput = true,
        //                    RedirectStandardError = true
        //                };

        //                using var process = Process.Start(startInfo);
        //                if (process == null) return false;

        //                string version = await process.StandardOutput.ReadToEndAsync();
        //                await process.WaitForExitAsync();

        //                // 9.x.x 버전인지 확인 (맨 앞자리가 9인지 체크)
        //                if (!string.IsNullOrWhiteSpace(version) && version.Trim().StartsWith("9"))
        //                {
        //                    return true;
        //                }

        //                // 만약 설치된 SDK 목록을 더 자세히 보고 싶다면 "dotnet --list-sdks"를 쓸 수도 있습니다.
        //                return false;
        //            }
        //            catch
        //            {
        //                // dotnet 명령어 자체가 인식되지 않는 경우 (SDK 미설치)
        //                return false;
        //            }
        //        }

        //        private async Task<bool> BuildDllsAsync()
        //        {
        //            // 1. .NET 9 SDK 설치 여부 확인
        //            UpdateProgressLevel("Checking .NET SDK Version...");
        //            bool isInstalled = await IsDotNetSdk9Installed();

        //            if (!isInstalled)
        //            {
        //                UpdateProgressLevel("Error: .NET 9 SDK not found.");

        //                // 메인 스레드(UI)에서 메시지 박스 출력
        //                Application.Current.Dispatcher.Invoke(() =>
        //                {
        //                    var result = MessageBox.Show(
        //                        ".NET 9.0 SDK가 설치되어 있지 않거나 경로를 찾을 수 없습니다.\n" +
        //                        "DLL 빌드를 위해 SDK 설치가 필요합니다. 다운로드 페이지로 이동하시겠습니까?",
        //                        ".NET 9 SDK Required",
        //                        MessageBoxButton.YesNo,
        //                        MessageBoxImage.Warning);

        //                    if (result == MessageBoxResult.Yes)
        //                    {
        //                        // 다운로드 페이지 자동 연결
        //                        Process.Start(new ProcessStartInfo("https://dotnet.microsoft.com/download/dotnet/9.0")
        //                        {
        //                            UseShellExecute = true
        //                        });
        //                    }
        //                });

        //                return false;
        //            }

        //            try
        //            {
        //                UpdateProgressLevel("Starting DLL Build Process...");

        //                // 1. 경로 설정 (SettingsWindow에서 받은 경로를 사용하는 것이 좋음)
        //                string dodSourcePath = Path.Combine(_baseOutputPath, "Data"); // DOD 코드 위치
        //                string parserSourcePath = Path.Combine(_baseOutputPath, "Parser"); // DTO/Parser 코드 위치
        //                string buildTempPath = Path.Combine(_projectRoot, "BuildTemp");

        //                if (!Directory.Exists(buildTempPath)) Directory.CreateDirectory(buildTempPath);

        //                // 2. DOD DLL 빌드 (유니티 런타임용)
        //                UpdateProgressLevel("Compiling DOD Runtime DLL...");
        //                bool dodResult = await ExecuteDllBuild(dodSourcePath, "Elder.Data.DOD", buildTempPath);

        //                // 3. DTO/Parser DLL 빌드 (에디터/툴용)
        //                UpdateProgressLevel("Compiling DTO/Parser Editor DLL...");
        //                bool parserResult = await ExecuteDllBuild(parserSourcePath, "Elder.Data.Parser", buildTempPath);

        //                if (dodResult && parserResult)
        //                {
        //                    UpdateProgressLevel("DLL Build Success! Moving to Unity...");
        //                    // TODO: 여기서 빌드된 DLL을 유니티 프로젝트 폴더로 복사하는 로직 추가
        //                    return true;
        //                }

        //                UpdateProgressLevel("DLL Build Failed. Check logs.");
        //                return false;
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine($"BuildDlls Error: {ex.Message}");
        //                return false;
        //            }
        //        }

        //        private async Task<bool> ExecuteDllBuild(string sourcePath, string assemblyName, string workingDir)
        //        {
        //            // 이 메서드는 소스 폴더를 기반으로 dotnet build 명령을 실행합니다.
        //            // 실제 구현 시 해당 폴더에 .csproj 파일이 존재해야 합니다.
        //            var startInfo = new ProcessStartInfo("dotnet", $"build -c Release -o \"{workingDir}\"")
        //            {
        //                WorkingDirectory = sourcePath, // .csproj가 있는 위치
        //                CreateNoWindow = true,
        //                UseShellExecute = false,
        //                RedirectStandardOutput = true
        //            };

        //            using var process = Process.Start(startInfo);
        //            if (process != null)
        //            {
        //                await process.WaitForExitAsync();
        //                return process.ExitCode == 0;
        //            }
        //            return false;
        //        }

        //        #region [Task Management & Dispose]
        //        private void RunTask(Func<Task> taskFunc)
        //        {
        //            if (_tasking) return;
        //            _tasking = true;
        //            Task.Run(async () => await RunTaskAsync(taskFunc));
        //        }

        //        private async Task RunTaskAsync(Func<Task> taskFunc)
        //        {
        //            try { await taskFunc.Invoke(); }
        //            finally { _tasking = false; }
        //        }

        //        public void Dispose()
        //        {
        //            Dispose(true);
        //            GC.SuppressFinalize(this);
        //        }

        //        protected virtual void Dispose(bool disposing)
        //        {
        //            if (!_disposed)
        //            {
        //                if (disposing)
        //                {
        //                    _disposables.Dispose();
        //                    ClearDocumentInfoCollection();
        //                    ClearDocumentInfos();
        //                }
        //                _disposed = true;
        //            }
        //        }

        //        private void ClearDocumentInfos()
        //        {
        //            foreach (var documentInfo in _documenttInfoDataMap.Values)
        //                documentInfo.Dispose();
        //            _documenttInfoDataMap.Clear();
        //        }

        //        private void ClearDocumentInfoCollection()
        //        {
        //            foreach (var documentInfo in DocumenttInfoDataCollection)
        //                documentInfo.Dispose();
        //            DocumenttInfoDataCollection.Clear();
        //        }

        //        private void ClearDocumentInfoDataMap()
        //        {
        //            foreach (var documentContent in _documentContents.Values)
        //                documentContent.Dispose();
        //            _documentContents.Clear();
        //        }

        //        public void ExportData()
        //        {

        //        }

        //        public void UpdateSettingsFromLocal()
        //        {
        //            _baseOutputPath = Settings.Default.BaseOutputPath;
        //            _rootNamespace = Settings.Default.RootNamespace;
        //        }

        //        ~DataForgeModel() => Dispose(false);
        //#endregion
    }
}