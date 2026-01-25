using System;
using Unity.Collections;

namespace Elder.Game.Resource.Data
{
	[Serializable]
	[MessagePack.MessagePackObject]
	public readonly struct MsgPTestSheetDOD
	{
		[MessagePack.Key(0)] public readonly FixedString32Bytes key; // Size: 4
		[MessagePack.Key(1)] public readonly int id; // Size: 1
		[MessagePack.Key(2)] public readonly int value; // Size: 1

		[MessagePack.SerializationConstructor]
		public MsgPTestSheetDOD(FixedString32Bytes key, int id, int value)
		{
			this.key = key;
			this.id = id;
			this.value = value;
		}
	}
}
