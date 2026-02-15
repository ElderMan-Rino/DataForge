namespace Elder.DataForge.Core.Common.Const.MessagePack
{
    public static class MessagePackConsts
    {
        public const string Prefix = "MsgPack";
        public const string DTOSuffix = "DTO";
        public const string DODSuffix = "DOD";
        public const string ResolverFileName = "MessagePackGeneratedResolver.cs";

        public const string DodProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>{0}</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup> 
    <PackageReference Include=""MessagePack"" Version=""2.5.140"" />
    <PackageReference Include=""Unity.Entities"" Version=""1.0.16"" />
    <PackageReference Include=""Unity.Collections"" Version=""1.2.4"" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include=""{1}\**\*.cs"" />
    <Compile Include=""{2}\**\*.cs"" />
  </ItemGroup>
</Project>";
    }
}
