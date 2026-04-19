using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct BlobTestSheetEditorData
	{
		[Key(1)] public readonly string key;
		[Key(0)] public readonly int id;
		[Key(2)] public readonly int value;

		[SerializationConstructor]
		public BlobTestSheetEditorData(int id, string key, int value)
		{
			this.id = id;
			this.key = key;
			this.value = value;
		}
	}
}
