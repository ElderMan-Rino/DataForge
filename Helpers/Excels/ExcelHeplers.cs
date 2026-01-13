using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.Helpers.Excels
{
    public static class ExcelHeplers
    {
        //public static string? GetCellValueAsString()
        //{
        //    if (cell == null || cell.Value2 == null)
        //        return convertNullToEmpty ? string.Empty : "null"; // 빈 셀 처리 (옵션)

        //    object cellValue = cell.Value2;
        //    if (cellValue is double dblValue)
        //    {
        //        // Excel 날짜 확인 (OADate 범위 체크)
        //        if (dblValue > 10000 && dblValue < 2958466) // 2958466 = 최대 OADate 값
        //        {
        //            return DateTime.FromOADate(dblValue).ToString("yyyy-MM-dd HH:mm:ss"); // 날짜 변환 후 문자열 반환
        //        }
        //        return dblValue.ToString(); // 숫자 변환 후 문자열 반환
        //    }
        //    else if (cellValue is bool boolValue)
        //    {
        //        return boolValue.ToString(); // 불리언 변환 후 문자열 반환
        //    }

        //    return cellValue.ToString(); // 문자열 그대로 반환
        //}
    }
}
