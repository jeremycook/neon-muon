using ClosedXML.Excel;
using System.Data;

namespace WebApiApp;

public class ExcelHelpers {
    public static DataTable ConvertWorksheetToDatabase(IXLWorksheet workSheet) {
        DataTable dataTable = new();

        var headerCells = workSheet.FirstRowUsed().CellsUsed();
        foreach (var cell in headerCells) {
            dataTable.Columns.Add(cell.Value.ToString().Trim());
        }

        var columnLetters = headerCells.Select(cell => cell.WorksheetColumn().ColumnLetter());

        foreach (var row in workSheet.RowsUsed().Skip(1)) {
            var dataRow = dataTable.Rows.Add();

            var i = -1;
            foreach (var columnLetter in columnLetters) {
                i++;
                dataRow[i] = row.Cell(columnLetter).Value.ToString();
            }
        }

        return dataTable;
    }
}
