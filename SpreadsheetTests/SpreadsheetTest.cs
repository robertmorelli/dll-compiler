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
        /// <summary>
        /// this was used for benchmarking search algorithms
        /// </summary>
        [TestMethod]
        public void BenchmarkLongChains()
        {
            Spreadsheet sheet = new();
            for (int i = 0; i<50; i++)
            {
                sheet.SetCellContents("a"+i, "a" + (i + 1));
            }
        }

        [TestMethod]
        public void ComplexDependencyTreeForOrderGaurentees()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("c1", "b1");
            sheet.SetCellContents("b1", "a1 + g1");
            sheet.SetCellContents("e1", "d1 + c1");
            sheet.SetCellContents("c1", "b1");
            sheet.SetCellContents("d1", "a1");
            sheet.SetCellContents("g1", "d1");

            sheet.SetCellContents("a1", "1");
            Assert.AreEqual(3.0, sheet.GetCellContents("e1"));
        }

        [TestMethod]
        public void TestMethod0()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("A1", "3");
            sheet.SetCellContents("B1", "A1 * A1");
            sheet.SetCellContents("C1", "B1 + A1");
            sheet.SetCellContents("D1", "C1 - B1");

        }

        [TestMethod]
        public void TestMethod1()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a4", "a3");
            sheet.SetCellContents("a3", "a2");
            sheet.SetCellContents("a2", "a1");
            sheet.SetCellContents("a1", 1);
            Assert.AreEqual(4,sheet.GetNamesOfAllNonemptyCells().Count());
            Assert.AreEqual(1.0, sheet.GetCellContents("a4"));
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void TestMethod2()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            sheet.SetCellContents("a2", "a3");
            sheet.SetCellContents("a3", "a1");
            throw new ArgumentException("");
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void TestMethod2and()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            sheet.SetCellContents("a3", "a4");
            sheet.SetCellContents("a4", "a2");
            sheet.SetCellContents("a2", "a3");
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void TestMethod2and2()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a1");
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
            Assert.AreEqual(typeof(FormulaError), sheet.GetCellContents("a1").GetType());
        }


        [TestMethod]
        public void TestMethod8()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "");
        }
    }
}