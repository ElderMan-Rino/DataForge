using Elder.DataForge.Core.CodeGenerators;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System.IO;
using System.Reactive.Subjects;
using System.Text;

namespace Elder.DataForge.Core.CodeSaver
{
    public class FileSourceCodeSaver : ISourceCodeSaver
    {
        private readonly Subject<string> _updateProgressLevel = new();
        private readonly Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public async Task<bool> ExportAsync(List<GeneratedSourceCode> sourceCodes)
        {
            try
            {
                if (sourceCodes == null || sourceCodes.Count == 0)
                {
                    UpdateProgressLevel("No source codes to save.");
                    UpdateProgressValue(100f);
                    return false;
                }

                UpdateProgressLevel($"Starting to save {sourceCodes.Count} source code files...");

                string outputDirectory = Settings.Default.OutputPath;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    UpdateProgressLevel("Error: Output path is not configured.");
                    return false;
                }

                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                for (int i = 0; i < sourceCodes.Count; i++)
                {
                    var code = sourceCodes[i];

                    // 카테고리별 폴더 생성
                    string folderPath = Path.Combine(outputDirectory, code.category.ToString());
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fullPath = Path.Combine(folderPath, code.fileName);

                    // 진행 메시지 업데이트
                    UpdateProgressLevel($"Saving [{code.category}] {code.fileName}...");

                    // 줄바꿈 정규화 및 파일 저장
                    string normalized = code.content.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    await File.WriteAllTextAsync(fullPath, normalized, Encoding.UTF8);

                    // 진행률 계산 및 업데이트 (0% ~ 100%)
                    float progress = (float)(i + 1) / sourceCodes.Count * 100f;
                    UpdateProgressValue(progress);
                }

                UpdateProgressLevel("All source code files have been saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                UpdateProgressLevel($"Save Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Save Error: {ex.Message}");
                return false;
            }
        }
    }
}