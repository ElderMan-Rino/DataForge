using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BootRow
	{
		public BlobString Label;
		public int Id;
		public AssetType AssetType;
	}
}
