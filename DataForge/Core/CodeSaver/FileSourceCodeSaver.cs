using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.CodeSaver
{
    public class FileSourceCodeSaver : ISourceCodeSaver
    {
        public async Task<bool> SaveAsync(List<GeneratedSourceCode> sourceCodes, string outputDirectory)
        {
            try
            {
                if (sourceCodes == null || sourceCodes.Count == 0)
                    return false;

                // 폴더가 없으면 생성
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                foreach (var code in sourceCodes)
                {
                    string fullPath = Path.Combine(outputDirectory, code.FileName);

                    // 1. 여기서 줄 바꿈 정규화 (CRLF 강제)를 처리합니다.
                    string normalized = code.Content.Replace("\r\n", "\n").Replace("\n", "\r\n");

                    // 2. UTF-8 with BOM으로 저장 (Visual Studio 경고 방지)
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