using System;
using System.Collections.Generic;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data
{
	[MessagePackObject]
	public readonly struct BlobSceneInfoEditorData
	{
		[Key(1)] public readonly string key;
		[Key(3)] public readonly string SceneKey;
		[Key(0)] public readonly int id;
		[Key(2)] public readonly SceneLoadType LoadMode;

		[SerializationConstructor]
		public BlobSceneInfoEditorData(int id, string key, SceneLoadType loadMode, string sceneKey)
		{
			this.id = id;
			this.key = key;
			this.LoadMode = loadMode;
			this.SceneKey = sceneKey;
		}
	}
}
