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
using System.Text;

namespace Elder.DataForge.Models
{
    internal class DataForgeModel : IModel
    {
        private IDocumentInfoLoader _infoLoader = new ExcelInfoLoader();
        private IDocumentContentExtracter _contentExtracter = new ExcelContentExtracter();
        private ISourceCodeGenerator _codeGenerator = new MessagePackSourceGenerator();
        private ISourceCodeSaver _codeSaver = new FileSourceCodeSaver();

        // [경로 설정] 프로젝트 루트와 출력 경로 정의
        private string _projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
        private string _baseOutputPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Resources\MessagePack"));

        private Dictionary<string, DocumentInfoData> _documenttInfoDataMap = new();
        private Dictionary<string, DocumentContentData> _documentContents = new();

        private CompositeDisposable _disposables = new();

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();

        private bool _disposed = false;
        private bool _tasking = false;

        public ObservableCollection<DocumentInfoData> DocumenttInfoDataCollection { get; private set; } = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        public DataForgeModel()
        {
            SubscribeToContentLoader();
            SubscribeToDataConverter();
            SubscribeToDataExporter();
        }

        #region [Setup & Automation 로직 추가]

        /// <summary>
        /// MPC 로컬 도구가 설치되어 있는지 확인하고 없으면 자동으로 설치합니다.
        /// </summary>
        private async Task<bool> EnsureMpcToolInstalled()
        {
            try
            {
                string manifestPath = Path.Combine(_projectRoot, ".config", "dotnet-tools.json");

                // 1. 매니페스트 확인 및 생성 (없으면 dotnet new tool-manifest)
                if (!File.Exists(manifestPath))
                {
                    UpdateProgressLevel("Initializing Tool Manifest...");
                    var startInfo = new ProcessStartInfo("dotnet", "new tool-manifest")
                    {
                        WorkingDirectory = _projectRoot,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(startInfo);
                    if (p != null) await p.WaitForExitAsync();
                }

                // 2. MPC 툴 로컬 설치 시도 (이미 있으면 에러가 나지만 무시하고 진행됨)
                UpdateProgressLevel("Syncing MessagePack Generator Tool...");
                var installInfo = new ProcessStartInfo("dotnet", "tool install MessagePack.Generator")
                {
                    WorkingDirectory = _projectRoot,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var pInstall = Process.Start(installInfo);
                if (pInstall != null) await pInstall.WaitForExitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tool Setup Error: {ex.Message}");
                return false;
            }
        }
        #endregion

        private void SubscribeToContentLoader()
        {
            _contentExtracter.OnProgressLevelUpdated.Subscribe(OnSourceProgressLevelUpdated).Add(_disposables);
            _contentExtracter.OnProgressValueUpdated.Subscribe(OnSourceProgressValueUpdated).Add(_disposables);
        }

        private void SubscribeToDataConverter() { }
        private void SubscribeToDataExporter() { }

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
        private void OnSourceProgressLevelUpdated(string progressLevel) => UpdateProgressLevel(progressLevel);
        private void OnSourceProgressValueUpdated(float progressValue) => UpdateProgressValue(progressValue);

        public void HandleLoadDocument()
        {
            ClearDocumentInfos();
            ClearDocumentInfoCollection();
            ClearDocumentInfoDataMap();
            LoadDocumentInfos();
        }

        private void LoadDocumentInfos()
        {
            if (!_infoLoader.TryLoadDocumentInfos(out var documentInfoData)) return;
            if (documentInfoData == null || !documentInfoData.Any()) return;

            foreach (var datum in documentInfoData)
            {
                _documenttInfoDataMap.Add(datum.Name, datum);
                DocumenttInfoDataCollection.Add(datum);
            }
        }

        public void CreateElements() => RunTask(CreateElementsAsync);

        private async Task<bool> ExportDataAsync()
        {
            var documentContents = await _contentExtracter.ExtractDocumentContentDataAsync(_documenttInfoDataMap.Values);
            if (documentContents == null || !documentContents.Any()) return false;

            foreach (var content in documentContents)
                _documentContents.Add(content.Key, content.Value);

            return true;
        }

        private async Task<bool> CreateElementsAsync()
        {
            // 1. 환경 체크 및 도구 설치 자동화
            UpdateProgressLevel("Checking Environment...");
            await EnsureMpcToolInstalled();

            // 2. 데이터 추출
            UpdateProgressLevel("Extracting Document Data...");
            var exportResult = await ExportDataAsync();
            if (!exportResult) return false;

            // 3. 소스 코드 생성
            UpdateProgressLevel("Generating Source Codes...");
            var generatedFiles = await _codeGenerator.GenerateAsync(_documentContents);
            if (generatedFiles == null || !generatedFiles.Any()) return false;

            // 4. 경로 설정 및 파일 분류 저장 (정규화 포함)
            string dataPath = Path.Combine(_baseOutputPath, "Data");
            string parserPath = Path.Combine(_baseOutputPath, "Parser");

            var dataFiles = generatedFiles.Where(f => !f.FileName.EndsWith(".Parser.cs")).ToList();
            var parserFiles = generatedFiles.Where(f => f.FileName.EndsWith(".Parser.cs")).ToList();

            UpdateProgressLevel("Saving Normalized Files...");
            bool s1 = await _codeSaver.SaveAsync(dataFiles, dataPath);
            bool s2 = await _codeSaver.SaveAsync(parserFiles, parserPath);

            if (!s1 || !s2) return false;

            // 5. MPC 실행 및 리졸버 생성
            UpdateProgressLevel("Running MessagePack Generator...");
            string mpcPath = Path.Combine(_baseOutputPath, "Mpc");
            if (!Directory.Exists(mpcPath)) Directory.CreateDirectory(mpcPath);

            // [해결] MSBuild 감지 실패 에러를 방지하는 RunMPC 실행
            bool mpcResult = await Task.Run(() => RunMPC(dataPath, mpcPath, "Elder.Framework.MessagePack.Generated"));

            if (mpcResult) UpdateProgressLevel("Framework Code Generation Completed.");
            else UpdateProgressLevel("MPC Compilation Failed. Please check console logs.");

            return mpcResult;
        }

        private bool RunMPC(string inputPath, string outputPath, string nameSpace)
        {
            try
            {
                // 1. MSBuild 실제 경로 탐색
                string msBuildPath = FindMsBuildPath();
                if (string.IsNullOrEmpty(msBuildPath))
                {
                    Debug.WriteLine("[MPC] Error: MSBuild.exe를 찾을 수 없습니다. VS 2022 설치 확인이 필요합니다.");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"tool run mpc -i \"{inputPath}\" -o \"{outputPath}\" -n \"{nameSpace}\"",
                    WorkingDirectory = _projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.GetEncoding("EUC-KR"),
                    StandardOutputEncoding = Encoding.GetEncoding("EUC-KR"),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 2. 환경 변수 강제 주입 (D드라이브 설치 시 필수)
                // MSBuild.exe의 상위 폴더에서 Sdks 폴더 경로를 유추합니다.
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
                        // 상세 에러 로그 출력
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
            // 사용자님이 확인해주신 D드라이브 경로를 최우선 순위로 둡니다.
            string[] searchPaths = {
        @"D:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        @"D:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        @"D:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private void RunTask(Func<Task> taskFunc)
        {
            if (_tasking) return;
            _tasking = true;
            Task.Run(async () => await RunTaskAsync(taskFunc));
        }

        private async Task RunTaskAsync(Func<Task> taskFunc)
        {
            await taskFunc.Invoke();
            _tasking = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ClearDocumentInfoCollection();
                    ClearDocumentInfos();
                    _disposables.Dispose();
                }
                _disposed = true;
            }
        }

        private void ClearDocumentInfos()
        {
            foreach (var documentInfo in _documenttInfoDataMap.Values)
                documentInfo.Dispose();
            _documenttInfoDataMap.Clear();
        }

        private void ClearDocumentInfoCollection()
        {
            foreach (var documentInfo in DocumenttInfoDataCollection)
                documentInfo.Dispose();
            DocumenttInfoDataCollection.Clear();
        }

        private void ClearDocumentInfoDataMap()
        {
            foreach (var documentContent in _documentContents.Values)
                documentContent.Dispose();
            _documentContents.Clear();
        }

        ~DataForgeModel() => Dispose(false);
    }
}