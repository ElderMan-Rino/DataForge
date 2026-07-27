using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct AssetInfoEntryRow
	{
		public BlobString Label;
		public int Id;
		public AssetType AssetType;
	}
}
