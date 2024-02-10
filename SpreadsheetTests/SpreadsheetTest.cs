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
using SS;

namespace SpreadsheetTests
{
    [TestClass]
    public class SpreadsheetTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            Spreadsheet sheet = new();
            Console.WriteLine(sheet.SetCellContents("a4", "a3").ToList().Aggregate("", (a, b) => a + b));
            Console.WriteLine(sheet.SetCellContents("a3", "a2").ToList().Aggregate("", (a, b) => a + b));
            Console.WriteLine(sheet.SetCellContents("a2", "a1").ToList().Aggregate("", (a, b) => a + b));
            Console.WriteLine(sheet.SetCellContents("a5", "a4").ToList().Aggregate("", (a, b) => a + b));
            Console.WriteLine(sheet.SetCellContents("a1", 1).ToList().Aggregate("", (a, b) => a + b));
            Console.WriteLine(sheet.GetNamesOfAllNonemptyCells().ToList().Aggregate("", (a, b) => a + b));
            Console.WriteLine(sheet.GetCellContents("a5"));
        }

        [TestMethod,ExpectedException(typeof(ArgumentException))]
        public void TestMethod2()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            sheet.SetCellContents("a2", "a3");
            sheet.SetCellContents("a3", "a1");
        }
    }
}