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
	public static class AudioSettingsBaker
	{
		public static void Bake(string sourcePath, string savePath, byte[] encryptionKeyPartB)
		{
			var rawBytes = File.ReadAllBytes(sourcePath);
			var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);
			var rawList = MessagePackSerializer.Deserialize<List<object[]>>(rawBytes, options);
			var dtoList = rawList.Select(row => new AudioSettings(System.Convert.ToInt32(row[0]), System.Convert.ToSingle(row[1]), System.Convert.ToSingle(row[2]), System.Convert.ToSingle(row[3]), System.Convert.ToSingle(row[4]), System.Convert.ToSingle(row[5]), System.Convert.ToBoolean(row[6]))).ToList();

			var builder = new BlobBuilder(Allocator.Temp);
			ref AudioSettingsRoot root = ref builder.ConstructRoot<AudioSettingsRoot>();
			var arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);

			for (int i = 0; i < dtoList.Count; i++)
			{
				arrayBuilder[i].Id = dtoList[i].Id;
				arrayBuilder[i].MasterVolume = dtoList[i].MasterVolume;
				arrayBuilder[i].BgmVolume = dtoList[i].BgmVolume;
				arrayBuilder[i].SfxVolume = dtoList[i].SfxVolume;
				arrayBuilder[i].VoiceVolume = dtoList[i].VoiceVolume;
				arrayBuilder[i].UiVolume = dtoList[i].UiVolume;
				arrayBuilder[i].MuteOnBackground = dtoList[i].MuteOnBackground;
			}

			var blobRef = builder.CreateBlobAssetReference<AudioSettingsRoot>(Allocator.Temp);
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
