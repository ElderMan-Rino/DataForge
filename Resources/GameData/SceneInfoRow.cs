using System;
using Unity.Burst;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	[BurstCompile]
	public struct SceneInfoRow
	{
		public BlobString Key;
		public BlobString SceneKey;
		public int Id;
		public SceneLoadType LoadMode;
	}
}
