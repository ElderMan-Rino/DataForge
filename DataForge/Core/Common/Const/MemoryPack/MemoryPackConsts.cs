namespace Elder.DataForge.Core.Common.Const.MemoryPack
{
    public static class MemoryPackConsts
    {
        public const string Prefix = "MsgPack";
        public const string DTOSuffix = "DTO";
        public const string DODSuffix = "DOD";
        public const string ResolverFileName = "MessagePackGeneratedResolver.cs";
        public const string Resolver = "Resolvers";

        public const string DodProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
          <PropertyGroup>
            <TargetFramework>netstandard2.1</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <AssemblyName>{0}</AssemblyName>
          </PropertyGroup>
          <ItemGroup> 
            <PackageReference Include=""MessagePack"" Version=""2.5.140"" />
          </ItemGroup>
        </Project>";
        
        public const string AssemblyName = "Elder.Generated.Data";
    }
}
