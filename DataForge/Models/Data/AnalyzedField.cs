namespace Elder.DataForge.Models.Data
{
    public record AnalyzedField(
         string Name, string PropertyName, string ManagedType, string UnmanagedType,
         int KeyIndex, int TotalSize, bool IsList, List<int> ExcelIndices);
}
