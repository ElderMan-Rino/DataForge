namespace Elder.DataForge.Core.Common.Const.MessagePack
{
    public static class MessagePackConsts
    {
        public const string Prefix = "MsgPack";
        public const string DTOSuffix = "EditorData";
        public const string DODSuffix = "GameData";
        public const string ResolverFileName = "MessagePackGeneratedResolver.cs";

        public const string DodProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>{0}</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Using Include=""Unity.Entities"" />
    <Using Include=""Unity.Collections"" />
  </ItemGroup>

  <ItemGroup> 
    <PackageReference Include=""MessagePack"" Version=""2.5.140"" />
    <PackageReference Include=""MessagePack.Annotations"" Version=""2.5.140"" />
  </ItemGroup>

  {2}

  <ItemGroup>
    {1}
  </ItemGroup>
</Project>";
    }
}
