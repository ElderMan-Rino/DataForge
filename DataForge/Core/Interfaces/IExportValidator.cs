using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    internal interface IExportValidator : IProgressNotifier
    {
        // 바이너리 데이터와 분석된 스키마를 비교 검증
        public Task<bool> ValidateAsync(byte[] binaryData, TableSchema schema);
    }
}
