using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct BootstrapDataRow
	{
		public BlobString Key;
		public BlobString DataKey;
		public int Id;
	}
}
