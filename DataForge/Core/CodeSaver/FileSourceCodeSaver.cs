using Elder.DataForge.Core.CodeGenerator;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.CodeSaver
{
    public class FileSourceCodeSaver : ISourceCodeSaver
    {
        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

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

                    // 이전처럼 무조건 OutputPath 하위 카테고리 폴더에 저장합니다.
                    string folderPath = Path.Combine(outputDirectory, code.category.ToString());
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fullPath = Path.Combine(folderPath, code.fileName);

                    UpdateProgressLevel($"Saving [{code.category}] {code.fileName}...");

                    string normalized = code.content.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    await File.WriteAllTextAsync(fullPath, normalized, Encoding.UTF8);

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