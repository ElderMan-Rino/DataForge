using Elder.Game.Resource.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Elder.Game.Resource.Data.Convert
{
	public static class MsgPTestSheetDTOParser
	{
		public static MsgPTestSheetDTO ParseRow(List<string> rowData)
		{
			var key = (rowData.Count > 3 && !string.IsNullOrEmpty(rowData[3])) ? rowData[3] : default;
			var id = (rowData.Count > 2 && !string.IsNullOrEmpty(rowData[2])) ? int.Parse(rowData[2]) : default;
			var value = (rowData.Count > 4 && !string.IsNullOrEmpty(rowData[4])) ? int.Parse(rowData[4]) : default;

			return new MsgPTestSheetDTO(key, id, value);
		}
	}
}
