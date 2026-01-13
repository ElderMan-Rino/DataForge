namespace Elder.DataForge.Models.Data
{
    public abstract class DocumentInfoData 
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
                
        public DocumentInfoData(string name, string path)
        {
            Name = name;
            Path = path;
        }
        public void Dispose()
        {
            Name = string.Empty;
            Path = string.Empty;
        }
    }
}
