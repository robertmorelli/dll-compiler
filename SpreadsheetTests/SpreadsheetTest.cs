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

        /// <summary>
        /// still not fully convinced of the correctness of the given version of
        /// the recalculation function but ya know this is probably good enough
        /// </summary>
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


        /// <summary>
        /// from docs just making sure everything works as intended
        /// </summary>
        [TestMethod]
        public void MakeSureEveryDeps()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("D1", "C1 - B1");
            sheet.SetCellContents("B1", "A1 * A1");
            sheet.SetCellContents("C1", "B1 + A1");
            Assert.AreEqual(true, sheet.SetCellContents("A1", "3").ToHashSet().SetEquals(["D1","C1","B1","A1"]));
            Assert.AreEqual(3.0, sheet.GetCellContents("D1"));
        }

        /// <summary>
        /// short chain check recalc
        /// </summary>
        [TestMethod]
        public void ShortChain()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a4", "a3");
            sheet.SetCellContents("a3", "a2");
            sheet.SetCellContents("a2", "a1");
            sheet.SetCellContents("a1", 1);
            Assert.AreEqual(4,sheet.GetNamesOfAllNonemptyCells().Count());
            Assert.AreEqual(1.0, sheet.GetCellContents("a4"));
        }

        /// <summary>
        /// simple circle
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        [TestMethod, ExpectedException(typeof(CircularException))]
        public void SimpleTriangle()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            sheet.SetCellContents("a2", "a3");
            sheet.SetCellContents("a3", "a1");
            throw new ArgumentException("");
        }

        /// <summary>
        /// hook loop is circular
        /// </summary>
        [TestMethod, ExpectedException(typeof(CircularException))]
        public void HookLoop()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            sheet.SetCellContents("a3", "a4");
            sheet.SetCellContents("a4", "a2");
            sheet.SetCellContents("a2", "a3");
        }

        /// <summary>
        /// direct self dependency
        /// </summary>
        [TestMethod, ExpectedException(typeof(CircularException))]
        public void SelfDep()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a1");
        }

        //TODO: make sure this is CORRECT
        /// <summary>
        /// make sure the formula exceptions pass through
        /// </summary>
        [TestMethod, ExpectedException(typeof(FormulaFormatException))]
        public void FormatExceptionPassThrough()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "+a2");
            sheet.GetCellContents("a1");
        }

        /// <summary>
        /// invalid name again
        /// </summary>
        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidNameGet()
        {
            Spreadsheet sheet = new();
            sheet.GetCellContents("--a1");
        }

        /// <summary>
        /// invalid name in set
        /// </summary>
        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidNameSet()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("--a1", "a2");
        }

        /// <summary>
        /// empty cell gives empty string
        /// </summary>
        [TestMethod]
        public void TestMethod6()
        {
            Spreadsheet sheet = new();
            Assert.AreEqual("",sheet.GetCellContents("a1"));
        }

        /// <summary>
        /// formula errror for dependency not being calculatable
        /// </summary>
        [TestMethod]
        public void FormulaErrorDepNotREal()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "a2");
            Assert.AreEqual(typeof(FormulaError), sheet.GetCellContents("a1").GetType());
        }

        /// <summary>
        /// empty string does not cause storage
        /// </summary>
        [TestMethod]
        public void emptyStringDontEnterValues()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", "");
            Assert.AreEqual(0,sheet.GetNamesOfAllNonemptyCells().Count());
        }
    }
}