using Elder.DataForge.Core.Common.Const.MessagePack;
using Elder.DataForge.Core.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Text;

namespace Elder.DataForge.Core.DLLBuilder
{
    public class DllBuilder : IDllBuilder
    {
        private readonly Subject<string> _updateProgressLevel = new();
        private readonly Subject<float> _updateProgressValue = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);

        public async Task<bool> BuildDllAsync(string sourceFolderPath, string outputDllPath)
        {
            try
            {
                UpdateProgressLevel("Preparing Project for Compilation...");

                string assemblyName = Path.GetFileNameWithoutExtension(outputDllPath);
                string csprojContent = string.Format(MessagePackConsts.DodProjectTemplate, assemblyName);

                string csprojPath = Path.Combine(sourceFolderPath, $"{assemblyName}.csproj");
                await File.WriteAllTextAsync(csprojPath, csprojContent, Encoding.UTF8);

                // 2. dotnet build 실행
                UpdateProgressLevel($"Compiling DLL: {assemblyName}...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{csprojPath}\" -c Release -o \"{Path.GetDirectoryName(outputDllPath)}\"",
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