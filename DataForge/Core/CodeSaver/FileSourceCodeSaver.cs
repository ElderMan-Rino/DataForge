using Elder.DataForge.Core.CodeGenerators;
using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Properties;
using System.IO;
using System.Text;

namespace Elder.DataForge.Core.CodeSaver
{
    public class FileSourceCodeSaver : ISourceCodeSaver
    {
        public async Task<bool> ExportAsync(List<GeneratedSourceCode> sourceCodes)
        {
            try
            {
                if (sourceCodes == null || sourceCodes.Count == 0)
                    return false;

                string outputDirectory = Settings.Default.OutputPath;
                if (string.IsNullOrEmpty(outputDirectory))
                    return false;

                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                foreach (var code in sourceCodes)
                {
                    string forlderPath = Path.Combine(outputDirectory, code.category.ToString());
                    if (!Directory.Exists(forlderPath))
                        Directory.CreateDirectory(forlderPath);

                    string fullPath = Path.Combine(forlderPath, code.fileName);
                    string normalized = code.content.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    await File.WriteAllTextAsync(fullPath, normalized, Encoding.UTF8);
                }
                return true;
            }
            catch (Exception ex)
            {
                // 로깅이 필요하다면 여기서 처리 (Console.WriteLine or Logger)
                System.Diagnostics.Debug.WriteLine($"Save Error: {ex.Message}");
                return false;
            }
        }
    }
}