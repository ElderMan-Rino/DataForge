namespace Elder.DataForge.Core.Commons.Enum
{
    public enum SourceCategory
    {
        Parser,
        EditorData,
        GameData,
        Utility,
        UnityScripts,   // Row, Root
        BlobLoader,     // ← 추가: GeneratedBlobLoader.cs 전용
        Enums,
        EditorScripts,
        SharedDTO
    }
}
