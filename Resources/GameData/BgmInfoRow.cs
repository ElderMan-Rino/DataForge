using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BgmInfoRow
	{
		public BlobString Key;
		public BlobString AssetName;
		public int Id;
	}
}
