using System;
using Unity.Entities;

namespace Elder.SkillTrial.Resources.Data
{
	public struct BlobSceneInfoGameData
	{
		public BlobString key;
		public BlobString SceneKey;
		public int id;
		public SceneLoadType LoadMode;
	}
}
