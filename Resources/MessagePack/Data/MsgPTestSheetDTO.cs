using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.Game.Resource.Data
{
	[MessagePackObject]
	public readonly struct MsgPTestSheetDTO
	{
		[Key(0)] public readonly string key;
		[Key(1)] public readonly int id;
		[Key(2)] public readonly int value;

		[SerializationConstructor]
		public MsgPTestSheetDTO(string key, int id, int value)
		{
			this.key = key;
			this.id = id;
			this.value = value;
		}
	}
}
