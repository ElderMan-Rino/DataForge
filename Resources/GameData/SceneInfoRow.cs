using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public struct SceneInfoRow
	{
		public BlobString Key;
		public BlobString SceneKey;
		public int Id;
		public SceneLoadType LoadMode;
	}
}
