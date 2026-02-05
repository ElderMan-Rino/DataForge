namespace Elder.DataForge.Core.PostProcessor
{
    public record PostProcessContext(string sourcePath, string mpcOutputPath, string targetNamespace, bool generateDll);
}
