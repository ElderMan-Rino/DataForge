using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct SplashEntryInfoRow
	{
		public BlobString SpriteName;
		public int Id;
		public float Interval;
	}
}
