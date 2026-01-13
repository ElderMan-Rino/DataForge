using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Models.Data
{
    public readonly struct ConversionResult
    {
        public readonly bool Status;
        public readonly IEnumerable<ConversionData> Result;

        public ConversionResult(bool status, IEnumerable<ConversionData> result)
        {
            Status = status;
            Result = result;
        }
    }
}
