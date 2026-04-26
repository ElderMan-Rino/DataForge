#if UNITY_EDITOR
using Elder.SkillTrial.Editor.Crypto;
using MessagePack;
using MessagePack.Resolvers;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace Elder.SkillTrial.Resources.Data
{
	public static class SceneInfoBaker
	{
		public static void Bake(string sourcePath, string savePath, byte[] encryptionKeyPartB)
		{
			var rawBytes = File.ReadAllBytes(sourcePath);
			var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);
			var dtoList = MessagePackSerializer.Deserialize<List<BlobSceneInfoEditorData>>(rawBytes, options);

			var builder = new BlobBuilder(Allocator.Temp);
			ref SceneInfoRoot root = ref builder.ConstructRoot<SceneInfoRoot>();
			var arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);

			for (int i = 0; i < dtoList.Count; i++)
			{
				builder.AllocateString(ref arrayBuilder[i].key, dtoList[i].key);
				builder.AllocateString(ref arrayBuilder[i].SceneKey, dtoList[i].SceneKey);
				arrayBuilder[i].id = dtoList[i].id;
				arrayBuilder[i].LoadMode = dtoList[i].LoadMode;
			}

			var blobRef = builder.CreateBlobAssetReference<SceneInfoRoot>(Allocator.Temp);
			builder.Dispose();

			var writer = new MemoryBinaryWriter();
			writer.Write(blobRef);

			unsafe
			{
				var plainSpan = new System.ReadOnlySpan<byte>(writer.Data, writer.Length);
				// [HEAP] 키 복사본 — AesEncryptionProvider 생성자가 원본을 ZeroMemory로 소거하므로 복사 필수
				byte[] keyPartBCopy = (byte[])encryptionKeyPartB.Clone();
				BlobEditorEncryptionHelper.WriteEncrypted(plainSpan, savePath, keyPartBCopy);
			}

			writer.Dispose();
			blobRef.Dispose();
		}
	}
}
#endif
