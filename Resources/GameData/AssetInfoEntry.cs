using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct AssetInfoEntry
	{
		[Key(0)] public readonly string Label;
		[Key(1)] public readonly int Id;
		[Key(2)] public readonly AssetType AssetType;

		[SerializationConstructor]
		public AssetInfoEntry(string label, int id, AssetType assetType)
		{
			this.Label = label;
			this.Id = id;
			this.AssetType = assetType;
		}
	}
}
