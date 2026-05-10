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
	public static class GraphicsSettingsBaker
	{
		public static void Bake(string sourcePath, string savePath, byte[] encryptionKeyPartB)
		{
			var rawBytes = File.ReadAllBytes(sourcePath);
			var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);
			var rawList = MessagePackSerializer.Deserialize<List<object[]>>(rawBytes, options);
			var dtoList = rawList.Select(row => new GraphicsSettings(System.Convert.ToInt32(row[0]), (FramerateType)System.Convert.ToInt32(row[1]), System.Convert.ToInt32(row[2]), (QualityLevel)System.Convert.ToInt32(row[3]), System.Convert.ToInt32(row[4]))).ToList();

			var builder = new BlobBuilder(Allocator.Temp);
			ref GraphicsSettingsRoot root = ref builder.ConstructRoot<GraphicsSettingsRoot>();
			var arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);

			for (int i = 0; i < dtoList.Count; i++)
			{
				arrayBuilder[i].Id = dtoList[i].Id;
				arrayBuilder[i].TargetFrameRate = dtoList[i].TargetFrameRate;
				arrayBuilder[i].VsyncCount = dtoList[i].VsyncCount;
				arrayBuilder[i].QualityLevel = dtoList[i].QualityLevel;
				arrayBuilder[i].ScreenSleepTimeout = dtoList[i].ScreenSleepTimeout;
			}

			var blobRef = builder.CreateBlobAssetReference<GraphicsSettingsRoot>(Allocator.Temp);
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
