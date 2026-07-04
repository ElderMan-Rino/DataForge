using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct BgmInfo
	{
		[Key(0)] public readonly string Key;
		[Key(1)] public readonly string AssetName;
		[Key(2)] public readonly int Id;

		[SerializationConstructor]
		public BgmInfo(string key, string assetName, int id)
		{
			this.Key = key;
			this.AssetName = assetName;
			this.Id = id;
		}
	}
}
