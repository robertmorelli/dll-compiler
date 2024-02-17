/// <summary>
/// Author:    Robert Morelli
/// Partner:   None
/// Date:      2-9-24
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and [Your Name(s)] - This work may not 
///            be copied for use in Academic Coursework.
///
/// I, Robert Morelli, certify that I wrote this code from scratch and
/// did not copy it in part or whole from code that is not my own. All 
/// references to code that is not my own used in the completion of the
/// assignments are cited in my README file.
///
/// File Contents:
/// My tests for my implementation of AbstractSpreadsheet
/// </summary>
using SpreadsheetUtilities;
using SS;

namespace SpreadsheetTests
{
    [TestClass]
    public class SpreadsheetTest
    {
        [TestMethod]
        public void BenchmarkLongChains()
        {
            Spreadsheet sheet = new((_)=>true, (s)=>s,"1");
            for (int i = 0; i < 12000; i++)
            {
                sheet.SetContentsOfCell("a" + i, "1");
                sheet.GetCellContents("a0");
                sheet.SetContentsOfCell("a" + i, "a" + (i + 1));
            }
        }
    }
}
