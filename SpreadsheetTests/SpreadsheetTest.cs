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
        public void TestMethod1()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a4", "a3");
            sheet.SetCellContents("a3", "a2");
            sheet.SetCellContents("a2", "a1");
            sheet.SetCellContents("a1", 1);
            Console.WriteLine(sheet.GetNamesOfAllNonemptyCells().Count());
            Console.WriteLine(sheet.GetCellContents("a4"));
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void TestMethod2()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            sheet.SetCellContents("a2", "a3");
            sheet.SetCellContents("a3", "a1");
        }


        
        [TestMethod, ExpectedException(typeof(FormulaFormatException))]
        public void TestMethod3()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "+a2");
            sheet.GetCellContents("a1");
        }
        
        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void TestMethod4()
        {
            Spreadsheet sheet = new();
            sheet.GetCellContents("--a1");
        }

        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void TestMethod5()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("--a1", "a2");
        }

        [TestMethod]
        public void TestMethod6()
        {
            Spreadsheet sheet = new();
            sheet.GetCellContents("a1");
        }

        [TestMethod]
        public void TestMethod7()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            Assert.AreEqual(typeof(FormulaError),sheet.GetCellContents("a1").GetType());
        }


        [TestMethod]
        public void TestMethod8()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "");
        }
    }
}