using System;
using Unity.Entities;

namespace 
{
	public struct SceneInfoRow
	{
		public BlobString Key;
		public BlobString SceneKey;
		public int Id;
		public SceneLoadType LoadMode;
	}
}
