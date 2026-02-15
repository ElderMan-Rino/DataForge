using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.CodeGenerator.MessagePack
{
    public static class MessagePackCompilerInvoker
    {
        public static void Run(string unityProjectPath, string inputSubPath, string outputSubPath, string nameSpace)
        {
            // 1. 경로 조합 (DOD 파일이 모여있는 곳과 리졸버가 생성될 곳)
            string inputPath = Path.Combine(unityProjectPath, inputSubPath);
            string outputPath = Path.Combine(unityProjectPath, outputSubPath);

            // 2. 출력 폴더가 없다면 생성
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // 3. MPC 명령어 인자 구성
            // -i: 입력 폴더, -o: 출력 파일명, -n: 네임스페이스
            string arguments = $"mpc -i \"{inputPath}\" -o \"{outputPath}\" -n \"{nameSpace}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet", // dotnet tool로 설치했을 경우
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"MPC Generation Failed: {error}");
                }
            }
        }
    }
}
