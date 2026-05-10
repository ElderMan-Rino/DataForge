#if UNITY_EDITOR
using Elder.Framework.Crypto;
using MessagePack;
using MessagePack.Resolvers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace Elder.SkillTrial.Resources.Data
{
	public static class LocaleSettingsBaker
	{
		public static void Bake(string sourcePath, string savePath, byte[] encryptionKeyPartB)
		{
			var rawBytes = File.ReadAllBytes(sourcePath);
			var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);
			var rawList = MessagePackSerializer.Deserialize<List<object[]>>(rawBytes, options);
			var dtoList = rawList.Select(row => new LocaleSettings(((System.Collections.IEnumerable)row[0]).Cast<object>().Select(x => (LanguageType)System.Convert.ToInt32(x)).ToList(), System.Convert.ToInt32(row[1]))).ToList();

			var builder = new BlobBuilder(Allocator.Temp);
			ref LocaleSettingsRoot root = ref builder.ConstructRoot<LocaleSettingsRoot>();
			var arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);

			for (int i = 0; i < dtoList.Count; i++)
			{
				var SupportedLanguagesBuilder = builder.Allocate(ref arrayBuilder[i].SupportedLanguages, dtoList[i].SupportedLanguages.Count);
				for (int j = 0; j < dtoList[i].SupportedLanguages.Count; j++) SupportedLanguagesBuilder[j] = dtoList[i].SupportedLanguages[j];
				arrayBuilder[i].Id = dtoList[i].Id;
			}

			var blobRef = builder.CreateBlobAssetReference<LocaleSettingsRoot>(Allocator.Temp);
			builder.Dispose();

			var writer = new MemoryBinaryWriter();
			writer.Write(blobRef);

			unsafe
			{
				var plainBytes = new byte[writer.Length];
				Marshal.Copy((System.IntPtr)writer.Data, plainBytes, 0, writer.Length);
				Elder.SkillTrial.Editor.Crypto.BlobEditorEncryptionHelper.WriteEncrypted(plainBytes, savePath, encryptionKeyPartB);
			}

			writer.Dispose();
			blobRef.Dispose();
		}
	}
}
#endif
