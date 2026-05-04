using Elder.SkillTrial.Resources.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data.Convert
{
	public static class BlobSceneInfoEditorDataParser
	{
		public static BlobSceneInfoEditorData ParseRow(List<string> rowData)
		{
			var key = (rowData.Count > 3 && !string.IsNullOrEmpty(rowData[3])) ? rowData[3] : default;
			var sceneKey = (rowData.Count > 5 && !string.IsNullOrEmpty(rowData[5])) ? rowData[5] : default;
			var id = (rowData.Count > 2 && !string.IsNullOrEmpty(rowData[2])) ? int.Parse(rowData[2]) : default;
			var loadMode = (rowData.Count > 4 && !string.IsNullOrEmpty(rowData[4])) ? ((SceneLoadType)Enum.Parse(typeof(SceneLoadType), rowData[4])) : default;

			return new BlobSceneInfoEditorData(key, sceneKey, id, loadMode);
		}
	}
}
