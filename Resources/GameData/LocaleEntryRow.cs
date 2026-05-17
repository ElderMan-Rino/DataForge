using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct LocaleEntryRow
	{
		public BlobString Key;
		public BlobString Value;
		public int Id;
	}
}
