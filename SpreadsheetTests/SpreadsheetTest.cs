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
            for (int i = 0; i < 70; i++)
            {
                sheet.SetContentsOfCell("a" + i, "1");
                sheet.GetCellContents("a0");
                sheet.SetContentsOfCell("a" + i, "=a" + (i + 1));
            }
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void CircularException()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a1", "=a2");
            sheet.SetContentsOfCell("a2", "=a3");
            sheet.SetContentsOfCell("a3", "=a1");
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void CircularException2()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a1", "=a1");
        }

        [TestMethod]
        public void AllContentTypes()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a0", "1");
            sheet.SetContentsOfCell("a1", "1");
            sheet.SetContentsOfCell("a2", "two");
            sheet.SetContentsOfCell("a3", "=3 + 3");
            sheet.GetCellContents("a0");
            sheet.GetCellContents("a1");
            sheet.GetCellContents("a2");
            sheet.GetCellContents("a3");

            sheet.GetCellValue("a3");
            sheet.GetNamesOfAllNonemptyCells();
            Console.WriteLine(sheet.GetXML());
            sheet.Save("AllContentTypes.xml");
            //Assert.AreEqual("1", sheet.GetSavedVersion("AllContentTypes.xml"));
        }

        [TestMethod,ExpectedException(typeof(InvalidNameException))]
        public void InvalidGetName()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellContents("--a3");
        }

        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidGetName2()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellValue("--a3");
        }

        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidSetName()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("--a3", "3 + 3");
        }

        [TestMethod]
        public void NoItem1()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellValue("a3");
        }

        [TestMethod]
        public void NoItem2()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellContents("a3");
        }

        [TestMethod]
        public void NoContent()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a3", "");
        }

        [TestMethod]
        public void StringContent()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a3", "hello");
            sheet.GetCellContents("a3");
        }

        [TestMethod]
        public void FormulaContent()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a3", "=6+8");
            sheet.GetCellContents("a3");
        }

        [TestMethod, ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void ReadFailure()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetSavedVersion("");
        }

        [TestMethod, ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SaveFailure()
        {
            Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.Save("");
        }

        [TestMethod]
        public void FourValueContructor()
        {
            var sheet = new Spreadsheet((_) => true, (s) => s, "1");
            for (int i = 0; i < 70; i++)
            {
                sheet.SetContentsOfCell("a" + i, "1");
                sheet.GetCellContents("a0");
                sheet.SetContentsOfCell("a" + i, "=a" + (i + 1));
            }
            sheet.Save("FourValueContructor");
            var sheet2 = new Spreadsheet("FourValueContructor", (_) => true, (s) => s, "1");
        }
    }
}
