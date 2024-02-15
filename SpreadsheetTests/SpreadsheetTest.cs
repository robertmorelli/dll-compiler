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
        /// change limit to 10k and expect to wait upwards of half an hour
        /// </summary>
        [TestMethod]
        public void BenchmarkLongChains()
        {
            Spreadsheet sheet = new();
            for (int i = 0; i < 70; i++)
            {
                sheet.SetCellContents("a" + i, 1);
                sheet.GetCellContents("a0");
                sheet.SetCellContents("a" + i, new Formula("a" + (i + 1)));
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
            sheet.SetCellContents("c1", new Formula("b1"));
            sheet.SetCellContents("b1", new Formula("a1 + g1"));
            sheet.SetCellContents("e1", new Formula("d1 + c1"));
            sheet.SetCellContents("c1", new Formula("b1"));
            sheet.SetCellContents("d1", new Formula("a1"));
            sheet.SetCellContents("g1", new Formula("d1"));

            sheet.SetCellContents("a1", 1);
            Assert.AreEqual(new Formula("d1 + c1"), sheet.GetCellContents("e1"));
        }


        /// <summary>
        /// from docs just making sure everything works as intended
        /// </summary>
        [TestMethod]
        public void MakeSureEveryDeps()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("D1", new Formula("C1 - B1"));
            sheet.SetCellContents("B1", new Formula("A1 * A1"));
            sheet.SetCellContents("C1", new Formula("B1 + A1"));
            Assert.AreEqual(true, sheet.SetCellContents("A1", 3).ToHashSet().SetEquals(["D1", "C1", "B1", "A1"]));
            Assert.AreEqual(new Formula("C1 - B1"), sheet.GetCellContents("D1"));
        }

        /// <summary>
        /// short chain check recalc
        /// </summary>
        [TestMethod]
        public void ShortChain()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a4", new Formula("a3"));
            sheet.SetCellContents("a3", new Formula("a2"));
            sheet.SetCellContents("a2", new Formula("a1"));
            sheet.SetCellContents("a1", 1);
            Assert.AreEqual(4, sheet.GetNamesOfAllNonemptyCells().Count());
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
            sheet.SetCellContents("a1", new Formula("a2"));
            sheet.SetCellContents("a2", new Formula("a3"));
            sheet.SetCellContents("a3", new Formula("a1"));
        }

        /// <summary>
        /// hook loop is circular
        /// </summary>
        [TestMethod, ExpectedException(typeof(CircularException))]
        public void HookLoop()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", new Formula("a2"));
            sheet.SetCellContents("a3", new Formula("a4"));
            sheet.SetCellContents("a4", new Formula("a2"));
            sheet.SetCellContents("a2", new Formula("a3"));
        }

        /// <summary>
        /// direct self dependency
        /// </summary>
        [TestMethod, ExpectedException(typeof(CircularException))]
        public void SelfDep()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", new Formula("a1"));
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
            sheet.SetCellContents("--a1", new Formula("a2"));
        }

        /// <summary>
        /// empty cell gives empty string
        /// </summary>
        [TestMethod]
        public void TestMethod6()
        {
            Spreadsheet sheet = new();
            Assert.AreEqual("", sheet.GetCellContents("a1"));
        }

        /// <summary>
        /// formula errror for dependency not being calculatable
        /// </summary>
        [TestMethod]
        public void FormulaErrorDepNotREal()
        {
            Spreadsheet sheet = new();
            sheet.SetCellContents("a1", new Formula("a2"));
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
            Assert.AreEqual(0, sheet.GetNamesOfAllNonemptyCells().Count());
        }
    }

    /// <summary>
    ///This is a test class for SpreadsheetTest and is intended
    ///to contain all SpreadsheetTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SpreadsheetTest2
    {


        [TestMethod(), Timeout(2000)]
        [TestCategory("2")]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestEmptyGetContents()
        {
            Spreadsheet s = new Spreadsheet();
            s.GetCellContents("1AA");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("3")]
        public void TestGetEmptyContents()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual("", s.GetCellContents("A2"));
        }

        // SETTING CELL TO A DOUBLE
        [TestMethod(), Timeout(2000)]
        [TestCategory("5")]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetInvalidNameDouble()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("1A1A", 1.5);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("6")]
        public void TestSimpleSetDouble()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("Z7", 1.5);
            Assert.AreEqual(1.5, (double)s.GetCellContents("Z7"), 1e-9);
        }

        // SETTING CELL TO A STRING
        [TestMethod(), Timeout(2000)]
        [TestCategory("9")]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetSimpleString()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("1AZ", "hello");
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("10")]
        public void TestSetGetSimpleString()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("Z7", "hello");
            Assert.AreEqual("hello", s.GetCellContents("Z7"));
        }

        // SETTING CELL TO A FORMULA
        [TestMethod(), Timeout(2000)]
        [TestCategory("13")]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetSimpleForm()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("1AZ", new Formula("2"));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("14")]
        public void TestSetGetForm()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("Z7", new Formula("3"));
            Formula f = (Formula)s.GetCellContents("Z7");
            Assert.AreEqual(new Formula("3"), f);
            Assert.AreNotEqual(new Formula("2"), f);
        }

        // CIRCULAR FORMULA DETECTION
        [TestMethod(), Timeout(2000)]
        [TestCategory("15")]
        [ExpectedException(typeof(CircularException))]
        public void TestSimpleCircular()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", new Formula("A2"));
            s.SetCellContents("A2", new Formula("A1"));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("16")]
        [ExpectedException(typeof(CircularException))]
        public void TestComplexCircular()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", new Formula("A2+A3"));
            s.SetCellContents("A3", new Formula("A4+A5"));
            s.SetCellContents("A5", new Formula("A6+A7"));
            s.SetCellContents("A7", new Formula("A1+A1"));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("17")]
        [ExpectedException(typeof(CircularException))]
        public void TestUndoCircular()
        {
            Spreadsheet s = new Spreadsheet();
            try
            {
                s.SetCellContents("A1", new Formula("A2+A3"));
                s.SetCellContents("A2", 15);
                s.SetCellContents("A3", 30);
                s.SetCellContents("A2", new Formula("A3*A1"));
            }
            catch (CircularException e)
            {
                Assert.AreEqual(15, (double)s.GetCellContents("A2"), 1e-9);
                throw e;
            }
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("17b")]
        [ExpectedException(typeof(CircularException))]
        public void TestUndoCellsCircular()
        {
            Spreadsheet s = new Spreadsheet();
            try
            {
                s.SetCellContents("A1", new Formula("A2"));
                s.SetCellContents("A2", new Formula("A1"));
            }
            catch (CircularException e)
            {
                Assert.AreEqual("", s.GetCellContents("A2"));
                Assert.IsTrue(new HashSet<string> { "A1" }.SetEquals(s.GetNamesOfAllNonemptyCells()));
                throw e;
            }
        }

        // NONEMPTY CELLS
        [TestMethod(), Timeout(2000)]
        [TestCategory("18")]
        public void TestEmptyNames()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.IsFalse(s.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("19")]
        public void TestExplicitEmptySet()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("B1", "");
            Assert.IsFalse(s.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("20")]
        public void TestSimpleNamesString()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("B1", "hello");
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("21")]
        public void TestSimpleNamesDouble()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("B1", 52.25);
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("22")]
        public void TestSimpleNamesFormula()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("B1", new Formula("3.5"));
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("23")]
        public void TestMixedNames()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", 17.2);
            s.SetCellContents("C1", "hello");
            s.SetCellContents("B1", new Formula("3.5"));
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "A1", "B1", "C1" }));
        }

        // RETURN VALUE OF SET CELL CONTENTS
        [TestMethod(), Timeout(2000)]
        [TestCategory("24")]
        public void TestSetSingletonDouble()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("B1", "hello");
            s.SetCellContents("C1", new Formula("5"));
            Assert.IsTrue(s.SetCellContents("A1", 17.2).SequenceEqual(new List<string>() { "A1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("25")]
        public void TestSetSingletonString()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", 17.2);
            s.SetCellContents("C1", new Formula("5"));
            Assert.IsTrue(s.SetCellContents("B1", "hello").SequenceEqual(new List<string>() { "B1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("26")]
        public void TestSetSingletonFormula()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", 17.2);
            s.SetCellContents("B1", "hello");
            Assert.IsTrue(s.SetCellContents("C1", new Formula("5")).SequenceEqual(new List<string>() { "C1" }));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("27")]
        public void TestSetChain()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", new Formula("A2+A3"));
            s.SetCellContents("A2", 6);
            s.SetCellContents("A3", new Formula("A2+A4"));
            s.SetCellContents("A4", new Formula("A2+A5"));
            Assert.IsTrue(s.SetCellContents("A5", 82.5).SequenceEqual(new List<string>() { "A5", "A4", "A3", "A1" }));
        }

        // CHANGING CELLS
        [TestMethod(), Timeout(2000)]
        [TestCategory("28")]
        public void TestChangeFtoD()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", new Formula("A2+A3"));
            s.SetCellContents("A1", 2.5);
            Assert.AreEqual(2.5, (double)s.GetCellContents("A1"), 1e-9);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("29")]
        public void TestChangeFtoS()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", new Formula("A2+A3"));
            s.SetCellContents("A1", "Hello");
            Assert.AreEqual("Hello", (string)s.GetCellContents("A1"));
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("30")]
        public void TestChangeStoF()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", "Hello");
            s.SetCellContents("A1", new Formula("23"));
            Assert.AreEqual(new Formula("23"), (Formula)s.GetCellContents("A1"));
            Assert.AreNotEqual(new Formula("24"), (Formula)s.GetCellContents("A1"));
        }

        // STRESS TESTS
        [TestMethod(), Timeout(2000)]
        [TestCategory("31")]
        public void TestStress1()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetCellContents("A1", new Formula("B1+B2"));
            s.SetCellContents("B1", new Formula("C1-C2"));
            s.SetCellContents("B2", new Formula("C3*C4"));
            s.SetCellContents("C1", new Formula("D1*D2"));
            s.SetCellContents("C2", new Formula("D3*D4"));
            s.SetCellContents("C3", new Formula("D5*D6"));
            s.SetCellContents("C4", new Formula("D7*D8"));
            s.SetCellContents("D1", new Formula("E1"));
            s.SetCellContents("D2", new Formula("E1"));
            s.SetCellContents("D3", new Formula("E1"));
            s.SetCellContents("D4", new Formula("E1"));
            s.SetCellContents("D5", new Formula("E1"));
            s.SetCellContents("D6", new Formula("E1"));
            s.SetCellContents("D7", new Formula("E1"));
            s.SetCellContents("D8", new Formula("E1"));
            ISet<String> cells = s.SetCellContents("E1", 0);
            Assert.IsTrue(new HashSet<string>() { "A1", "B1", "B2", "C1", "C2", "C3", "C4", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "E1" }.SetEquals(cells));
        }

        // Repeated for extra weight
        [TestMethod(), Timeout(2000)]
        [TestCategory("32")]
        public void TestStress1a()
        {
            TestStress1();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("33")]
        public void TestStress1b()
        {
            TestStress1();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("34")]
        public void TestStress1c()
        {
            TestStress1();
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("35")]
        public void TestStress2()
        {
            Spreadsheet s = new Spreadsheet();
            ISet<String> cells = new HashSet<string>();
            for (int i = 1; i < 200; i++)
            {
                cells.Add("A" + i);
                Assert.IsTrue(cells.SetEquals(s.SetCellContents("A" + i, new Formula("A" + (i + 1)))));
            }
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("36")]
        public void TestStress2a()
        {
            TestStress2();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("37")]
        public void TestStress2b()
        {
            TestStress2();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("38")]
        public void TestStress2c()
        {
            TestStress2();
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("39")]
        public void TestStress3()
        {
            Spreadsheet s = new Spreadsheet();
            for (int i = 1; i < 200; i++)
            {
                s.SetCellContents("A" + i, new Formula("A" + (i + 1)));
            }
            try
            {
                s.SetCellContents("A150", new Formula("A50"));
                Assert.Fail();
            }
            catch (CircularException)
            {
            }
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("40")]
        public void TestStress3a()
        {
            TestStress3();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("41")]
        public void TestStress3b()
        {
            TestStress3();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("42")]
        public void TestStress3c()
        {
            TestStress3();
        }








        [TestMethod(), Timeout(2000)]
        [TestCategory("43")]
        public void TestStress4()
        {
            Spreadsheet s = new Spreadsheet();
            for (int i = 0; i < 500; i++)
            {
                s.SetCellContents("A1" + i, new Formula("A1" + (i + 1)));
            }
            LinkedList<string> firstCells = new LinkedList<string>();
            LinkedList<string> lastCells = new LinkedList<string>();
            for (int i = 0; i < 250; i++)
            {
                firstCells.AddFirst("A1" + i);
                lastCells.AddFirst("A1" + (i + 250));
            }
            Assert.IsTrue(s.SetCellContents("A1249", 25.0).SetEquals(firstCells));
            Assert.IsTrue(s.SetCellContents("A1499", 0).SetEquals(lastCells));
        }

















        [TestMethod(), Timeout(2000)]
        [TestCategory("44")]
        public void TestStress4a()
        {
            TestStress4();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("45")]
        public void TestStress4b()
        {
            TestStress4();
        }
        [TestMethod(), Timeout(2000)]
        [TestCategory("46")]
        public void TestStress4c()
        {
            TestStress4();
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("47")]
        public void TestStress5()
        {
            RunRandomizedTest(47, 2519);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("48")]
        public void TestStress6()
        {
            RunRandomizedTest(48, 2521);
        }

        [TestMethod(), Timeout(2000)]
        [TestCategory("49")]
        public void TestStress7()
        {
            RunRandomizedTest(49, 2526);
        }

        [TestMethod()]//, Timeout(2000)]
        [TestCategory("50")]
        public void TestStress8()
        {
            RunRandomizedTest(50, 2521);
        }

        /// <summary>
        /// Sets random contents for a random cell 10000 times
        /// </summary>
        /// <param name="seed">Random seed</param>
        /// <param name="size">The known resulting spreadsheet size, given the seed</param>
        public void RunRandomizedTest(int seed, int size)
        {
            Spreadsheet s = new Spreadsheet();
            Random rand = new Random(seed);
            for (int i = 0; i < 10000; i++)
            {
                try
                {
                    switch (rand.Next(3))
                    {
                        case 0:
                            s.SetCellContents(randomName(rand), 3.14);
                            break;
                        case 1:
                            s.SetCellContents(randomName(rand), "hello");
                            break;
                        case 2:
                            s.SetCellContents(randomName(rand), randomFormula(rand));
                            break;
                    }
                }
                catch (CircularException)
                {
                }
            }
            ISet<string> set = new HashSet<string>(s.GetNamesOfAllNonemptyCells());
            Assert.AreEqual(size, set.Count);
        }

        /// <summary>
        /// Generates a random cell name with a capital letter and number between 1 - 99
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        private String randomName(Random rand)
        {
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(rand.Next(26), 1) + (rand.Next(99) + 1);
        }

        /// <summary>
        /// Generates a random Formula
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        private String randomFormula(Random rand)
        {
            String f = randomName(rand);
            for (int i = 0; i < 10; i++)
            {
                switch (rand.Next(4))
                {
                    case 0:
                        f += "+";
                        break;
                    case 1:
                        f += "-";
                        break;
                    case 2:
                        f += "*";
                        break;
                    case 3:
                        f += "/";
                        break;
                }
                switch (rand.Next(2))
                {
                    case 0:
                        f += 7.2;
                        break;
                    case 1:
                        f += randomName(rand);
                        break;
                }
            }
            return f;
        }

    }
}
