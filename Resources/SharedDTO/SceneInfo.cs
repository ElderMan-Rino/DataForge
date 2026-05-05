using System;
using System.Collections.Generic;
using MessagePack;

namespace 
{
	[MessagePackObject]
	public readonly struct SceneInfo
	{
		[Key(0)] public readonly string Key;
		[Key(1)] public readonly string SceneKey;
		[Key(2)] public readonly int Id;
		[Key(3)] public readonly SceneLoadType LoadMode;

		[SerializationConstructor]
		public SceneInfo(string key, string sceneKey, int id, SceneLoadType loadMode)
		{
			this.Key = key;
			this.SceneKey = sceneKey;
			this.Id = id;
			this.LoadMode = loadMode;
		}
	}
}
