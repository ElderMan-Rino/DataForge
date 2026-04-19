#if UNITY_EDITOR
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Resolvers;
using Unity.Entities;
using Unity.Collections;
using Unity.Entities.Serialization;

namespace Elder.SkillTrial.Resources.Data
{
	public static class TestSheetBaker
	{
		public static void Bake(string sourcePath, string savePath)
		{
			var rawBytes = File.ReadAllBytes(sourcePath);
			var resolver = MessagePack.Resolvers.CompositeResolver.Create(
				MessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,
				MessagePack.Resolvers.StandardResolver.Instance
			);
			var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
			var dtoList = MessagePackSerializer.Deserialize<List<BlobTestSheetEditorData>>(rawBytes, options);

			var builder = new BlobBuilder(Allocator.Temp);
			ref TestSheetRoot root = ref builder.ConstructRoot<TestSheetRoot>();
			var arrayBuilder = builder.Allocate(ref root.Rows, dtoList.Count);

			for (int i = 0; i < dtoList.Count; i++)
			{
				builder.AllocateString(ref arrayBuilder[i].key, dtoList[i].key);
				arrayBuilder[i].id = dtoList[i].id;
				arrayBuilder[i].value = dtoList[i].value;
			}

			var blobRef = builder.CreateBlobAssetReference<TestSheetRoot>(Allocator.Temp);
			builder.Dispose(); // 빌더 수동 해제 (ref 보호 에러 해결)

			var writer = new MemoryBinaryWriter();
			writer.Write(blobRef);

			unsafe
			{
				var span = new ReadOnlySpan<byte>(writer.Data, writer.Length);
				using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
				fileStream.Write(span);
			}

			writer.Dispose();
			blobRef.Dispose();
		}
	}
}
#endif
