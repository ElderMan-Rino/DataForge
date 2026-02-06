namespace Elder.DataForge.Core.PostProcessor
{
    public record PostProcessContext(string sourcePath, string targetNamespace, bool generateDll);
}
