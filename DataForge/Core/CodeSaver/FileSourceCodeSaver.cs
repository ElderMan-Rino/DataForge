using Elder.DataForge.Core.CodeGenerator;
using Elder.DataForge.Core.Commons.Enum;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;
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

        // 저장 전 클리어할 카테고리 — 매 생성마다 최신 파일만 유지
        private static readonly SourceCategory[] _clearBeforeSave =
        {
            SourceCategory.GameData,
            SourceCategory.SharedDTO,
            SourceCategory.EditorScripts,
            SourceCategory.Enums,
            SourceCategory.BlobLoader,
        };

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

                string outputDirectory = Settings.Default.OutputPath;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    UpdateProgressLevel("Error: Output path is not configured.");
                    return false;
                }

                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                // ─── 저장 전 대상 카테고리 폴더 클리어 ──────────────────────
                // 이전 생성 파일(수동 파일 포함)이 남아서 중복 타입 충돌이 생기는 것을 방지
                var newCategories = sourceCodes.Select(c => c.category).ToHashSet();
                foreach (var category in _clearBeforeSave)
                {
                    if (!newCategories.Contains(category)) continue;

                    string folderPath = Path.Combine(outputDirectory, category.ToString());
                    if (Directory.Exists(folderPath))
                    {
                        UpdateProgressLevel($"Clearing [{category}] folder...");
                        Directory.Delete(folderPath, recursive: true);
                    }
                }

                // ─── 파일 저장 ────────────────────────────────────────────
                UpdateProgressLevel($"Starting to save {sourceCodes.Count} source code files...");

                for (int i = 0; i < sourceCodes.Count; i++)
                {
                    var code = sourceCodes[i];

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
                UpdateProgressLevel($"Error saving source code: {ex.Message}");
                return false;
            }
        }
    }
}