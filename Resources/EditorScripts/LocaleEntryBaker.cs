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

namespace 
{
	public static class LocaleEntryBaker
	{
		private static List<LocaleEntry> ParseDto(string sourcePath)
		{
			var rawBytes = File.ReadAllBytes(sourcePath);
			var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);
			var rawList = MessagePackSerializer.Deserialize<List<object[]>>(rawBytes, options);
			return rawList.Select(row => new LocaleEntry(row[0]?.ToString() ?? string.Empty, row[1]?.ToString() ?? string.Empty, System.Convert.ToInt32(row[2]))).ToList();
		}

		private static void SaveBlob(
			BlobAssetReference<LocaleEntryRoot> blobRef,
			string savePath,
			byte[] encryptionKeyPartB)
		{
			var writer = new MemoryBinaryWriter();
			writer.Write(blobRef);
			unsafe
			{
				var plainBytes = new byte[writer.Length];
				System.Runtime.InteropServices.Marshal.Copy((System.IntPtr)writer.Data, plainBytes, 0, writer.Length);
				Elder.SkillTrial.Editor.Crypto.BlobEditorEncryptionHelper.WriteEncrypted(plainBytes, savePath, encryptionKeyPartB);
			}
			writer.Dispose();
			blobRef.Dispose();
		}

		private static void BakeInternal(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
		{
			var dtoList = ParseDto(sourcePath);
			var builder = new BlobBuilder(Allocator.Temp);
			ref LocaleEntryRoot root = ref builder.ConstructRoot<LocaleEntryRoot>();
			var arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);
			for (int i = 0; i < dtoList.Count; i++)
			{
				builder.AllocateString(ref arrayBuilder[i].Key, dtoList[i].Key);
				builder.AllocateString(ref arrayBuilder[i].Value, dtoList[i].Value);
				arrayBuilder[i].Id = dtoList[i].Id;
			}
			var blobRef = builder.CreateBlobAssetReference<LocaleEntryRoot>(Allocator.Temp);
			builder.Dispose();
			SaveBlob(blobRef, savePath, encryptionKeyPartB);
		}

		// BootstrapLocale_Ko 시트 진입점
		public static void Bake_BootstrapLocale_Ko(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
			=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);

		// BootstrapLocale_Jp 시트 진입점
		public static void Bake_BootstrapLocale_Jp(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
			=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);

		// BootstrapLocale_En 시트 진입점
		public static void Bake_BootstrapLocale_En(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
			=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);

		// ErrorMsgLocale_Ko 시트 진입점
		public static void Bake_ErrorMsgLocale_Ko(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
			=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);

		// ErrorMsgLocale_Jp 시트 진입점
		public static void Bake_ErrorMsgLocale_Jp(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
			=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);

		// ErrorMsgLocale_En 시트 진입점
		public static void Bake_ErrorMsgLocale_En(
			string sourcePath, string savePath, byte[] encryptionKeyPartB)
			=> BakeInternal(sourcePath, savePath, encryptionKeyPartB);

	}
}
#endif
