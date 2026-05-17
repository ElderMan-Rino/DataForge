using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct ErrorCodeRow
	{
		public BlobString Key;
		public BlobString LocaleKey;
		public int Id;
		public ErrorCategory Category;
		public ErrorActionType Action;
	}
}
