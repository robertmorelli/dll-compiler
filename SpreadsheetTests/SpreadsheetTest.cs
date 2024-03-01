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

using System.Reflection;
using SS;
namespace SpreadsheetTests
{
    [TestClass]
    public class SpreadsheetTest
    {
        [TestMethod]
        public void BenchmarkLongChains()
        {
            
            Spreadsheet.Spreadsheet sheet = new();
            sheet.SetContentsOfCell("a0", "1");
            sheet.SetContentsOfCell("a1", "2");
            sheet.SetContentsOfCell("a2", "3");
            sheet.SetContentsOfCell("a3", "4");
            sheet.SetContentsOfCell("a4", "1");
            for (int i = 100; i > 4; i--)
                sheet.SetContentsOfCell("a" + i,
                    string.Format(
                         "=(25 * 50 - 100 + 200 / 400) + a{0} * a{1} - a{2} + a{3} / a{4}",
                         i - 1, i - 2, i - 3, i - 4, i - 5
                        )
                    );
        }

        [TestMethod]
        public void BenchmarkLongChainsBackwards()
        {
            Spreadsheet.Spreadsheet sheet = new();
            sheet.SetContentsOfCell("a0", "1");
            sheet.SetContentsOfCell("a1", "2");
            sheet.SetContentsOfCell("a2", "3");
            sheet.SetContentsOfCell("a3", "4");
            sheet.SetContentsOfCell("a4", "1");
            for (int i = 5; i < 100; i++)
                sheet.SetContentsOfCell("a" + i,
                    string.Format(
                         "=(25 * 50 - 100 + 200 / 400) + a{0} * a{1} - a{2} + a{3} / a{4}",
                         i - 1, i - 2, i - 3, i - 4, i - 5
                        )
                    );

            sheet.Compile("name");
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void CircularException()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a1", "=a2");
            sheet.SetContentsOfCell("a2", "=a3");
            sheet.SetContentsOfCell("a3", "=a1");
        }

        [TestMethod, ExpectedException(typeof(CircularException))]
        public void CircularException2()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a1", "=a1");
        }

        [TestMethod]
        public void AllContentTypes()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
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

        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidGetName()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellContents("--a3");
        }

        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidGetName2()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellValue("--a3");
        }

        [TestMethod, ExpectedException(typeof(InvalidNameException))]
        public void InvalidSetName()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("--a3", "3 + 3");
        }

        [TestMethod]
        public void NoItem1()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellValue("a3");
        }

        [TestMethod]
        public void NoItem2()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetCellContents("a3");
        }

        [TestMethod]
        public void NoContent()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a3", "");
        }

        [TestMethod]
        public void StringContent()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a3", "hello");
            sheet.GetCellContents("a3");
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        [TestMethod]
        public void FormulaContent()
        {
            Spreadsheet.Spreadsheet sheet = new();
            sheet.SetContentsOfCell("a4", "=  a3 + 20 * 10");
            sheet.SetContentsOfCell("a3", "=  a2 + 6 * 30");
            sheet.SetContentsOfCell("a2", "= a1 / 2");
            sheet.SetContentsOfCell("a1", "4");
            sheet.GetCellContents("a3");
            
            //compile to location
            var location = sheet.Compile("name");
            
            //load back in and get type data
            var nameAssembly = Assembly.LoadFile(location);
            var instance = nameAssembly.CreateInstance("sheetSpace.sheetLibrary");
            var sheetType = instance?.GetType();
            
            //get methods to test
            var getA4 = sheetType?.GetMethod("Get_a4");
            var setA1 = sheetType?.GetMethod("Put_a1");
            
            //check constructor puts default values
            Assert.AreEqual(sheet.GetCellValue("a4"), getA4?.Invoke(instance, []));
            
            //change value for both
            setA1?.Invoke(instance, [5]);
            sheet.SetContentsOfCell("a1", "5");
            
            //check values are updated properly
            Assert.AreEqual(sheet.GetCellValue("a4"), getA4?.Invoke(instance, []));
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        [TestMethod, ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void ReadFailure()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.GetSavedVersion("");
        }

        [TestMethod, ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SaveFailure()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.Save("");
        }

        [TestMethod]
        public void SaveREtreive()
        {
            Spreadsheet.Spreadsheet sheet = new((_) => true, (s) => s, "1");
            sheet.SetContentsOfCell("a1", "hi");
            sheet.Save("SaveREtreive");
            Console.WriteLine(sheet.GetSavedVersion("SaveREtreive"));
            Spreadsheet.Spreadsheet sheet2 = new("SaveREtreive", (_) => true, (s) => s, "1");
            Console.WriteLine(sheet.GetCellContents("a1"));
        }

        [TestMethod, ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SavedVersionFailure()
        {
            Spreadsheet.Spreadsheet sheet = new();
            sheet.GetSavedVersion("SavedVersionFailure;lkdfgj;adklfjg;slkdfjg;lskdfjg;klsdjfg;klsjdf;klgjsdf;klg");
        }

        [TestMethod]
        public void FourValueContructor()
        {
            var sheet = new Spreadsheet.Spreadsheet();
            for (int i = 0; i < 70; i++)
            {
                sheet.SetContentsOfCell("a" + i, "1");
                sheet.GetCellContents("a0");
                sheet.SetContentsOfCell("a" + i, "=a" + (i + 1));
            }
            sheet.Save("FourValueContructor");
            var sheet2 = new Spreadsheet.Spreadsheet("FourValueContructor", (_) => true, (s) => s, "1");
        }

        [TestMethod]
        public void ChangedCheck()
        {
            var sheet = new Spreadsheet.Spreadsheet();
            sheet.SetContentsOfCell("a1", "hi");
            sheet.GetCellValue("a1");
            Assert.IsTrue(sheet.Changed);
        }
    }
}
