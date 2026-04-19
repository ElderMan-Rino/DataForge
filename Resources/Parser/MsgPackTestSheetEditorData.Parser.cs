using Elder.SkillTrial.Resources.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Elder.SkillTrial.Resources.Data.Convert
{
	public static class MsgPackTestSheetEditorDataParser
	{
		public static MsgPackTestSheetEditorData ParseRow(List<string> rowData)
		{
			var key = (rowData.Count > 3 && !string.IsNullOrEmpty(rowData[3])) ? rowData[3] : default;
			var id = (rowData.Count > 2 && !string.IsNullOrEmpty(rowData[2])) ? int.Parse(rowData[2]) : default;
			var value = (rowData.Count > 4 && !string.IsNullOrEmpty(rowData[4])) ? int.Parse(rowData[4]) : default;

			return new MsgPackTestSheetEditorData(id, key, value);
		}
	}
}
