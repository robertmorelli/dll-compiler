/// <summary>
/// Author:    Robert Morelli
/// Partner:   None
/// Date:      2-8-24
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
///tests for the dependency graph
/// first 20 tests should test every comination of paths through the code
/// this file also includes a random valid and invalid
/// formulas
/// </summary>
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using System.Text.RegularExpressions;

namespace FormulaTests
{
    [TestClass]
    public class FormulaTests
    {

        private delegate string GetAChild();
        /// <summary>
        /// Generate a viable test case using a cfg
        /// The code is pretty obvious if you know what a CFG is supposed to look like
        /// I will not give an explanation of a CFG here
        /// </summary>
        /// <param name="lookupDict">
        /// a dictionary to be passed to the evaluator.
        /// </param>
        /// <param name="errorVar">
        /// bool to add nonexistent var to fromula
        /// </param>
        /// <returns></returns>
        static string createTestCase(Dictionary<string, int> lookupDict, bool errorVar)
        {
            Random random = new Random();
            int expansions = 1000;
            var CFG = new Dictionary<string, List<GetAChild>> {
                {"&S",new List<GetAChild>{() => "&E" } },
                {"&E" ,new List<GetAChild>{
                    () => "&T",
                    () => "(&W&T&W)",
                    () => "&T&W&O&W&T",
                    () => {
                        expansions--;
                        return "&E&W&O&W&T";
                    },
                    () => {
                        expansions--;
                        return "&T&W&O&W&E";
                    },
                    () => {
                        expansions--;
                        return "(&W&E&W)";
                    },
                    () => {
                        expansions-=2;
                        return "&E&W&O&W&E";
                    },
                    () => {
                        expansions-=2;
                        return "&E&W&O&W&E";
                    },
                    () => {
                        expansions-=2;
                        return "&E&W&O&W&E";
                    },
                }},
                {"&T" ,new List<GetAChild>{
                    () => random.Next(1, 1000).ToString(),
                    () => {
                        string key = GenerateRandomAlphabeticString(random.Next(1, 4))+random.Next(1, 1000).ToString();
                        if (lookupDict.ContainsKey(key))
                        {
                            return key;
                        }
                        else
                        {
                            int val = random.Next(1, 1000);
                            lookupDict.Add(key, val);
                            return key;
                        }
                    },
                }},
                {"&O" ,new List<GetAChild>{
                    () => "+",
                    () => "-",
                    () => "/",
                    () => "*",
                }},

                {"&W" ,new List<GetAChild>{
                    () => {
                        string space = "";
                        for(int i = 0; i < random.Next(0,4); i++)
                        {
                            space += " ";
                        }
                        return space;
                    },
                }},
            };

            string exp = "&S";
            var tokenIdentifier = new Regex("(&[WSTOE])");
            while (exp.Contains("&"))
            {
                var tokens = Regex.Split(exp, tokenIdentifier.ToString());
                exp = "";
                foreach (var token in tokens)
                {
                    if (tokenIdentifier.IsMatch(token))
                    {
                        if (token == "&T" && errorVar)
                        {
                            exp += " a0 ";
                            errorVar = false;
                        }

                        var possibilities = CFG.GetValueOrDefault(token, [() => "1"]);
                        if (expansions < 0)
                        {
                            exp += possibilities[0].Invoke();
                        }
                        else
                        {
                            exp += possibilities[random.Next(0, possibilities.Count())].Invoke();
                        }
                    }
                    else
                    {
                        exp += token;
                    }
                }
            }
            return exp;
        }

        /// <summary>
        /// makes random alphabetic strings via the most niavest algorithm
        /// </summary>
        /// <param name="length">
        /// how long should it be
        /// </param>
        /// <returns>
        /// the random string
        /// </returns>
        static string GenerateRandomAlphabeticString(int length)
        {
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            Random random = new Random();
            char[] randomChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                int randomIndex = random.Next(0, alphabet.Length);
                randomChars[i] = alphabet[randomIndex];
            }
            return new string(randomChars);
        }

        //DIVIDE BY ZERO
        [ExpectedException(typeof(FormulaFormatException))]
        public void DivByZeroConst()
        {
            var a = new Formula("a1/0");
            a.Evaluate((_) => 1);
        }

        [ExpectedException(typeof(FormulaFormatException))]
        public void DivByZeroConstParens()
        {
            var a = new Formula("a1/(0)");
            a.Evaluate((_) => 1);
        }

        [ExpectedException(typeof(FormulaFormatException))]
        public void DivByZeroVar()
        {
            var a = new Formula("5/(1-a1)");
            a.Evaluate((_) => 1);
        }

        //OPTIMIZATIONS
        [TestMethod]
        public void PreEvaluation1()
        {
            var a = new Formula("(5+5)*a1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void PreEvaluation2()
        {
            var a = new Formula("(5+5)+a1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void PreEvaluation3()
        {
            var a = new Formula("(5+5)/a1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void PreEvaluation4()
        {
            var a = new Formula("(5+5)-a1");
            a.Evaluate((_) => 1);
        }


        [TestMethod]
        public void DoNothingOperations1()
        {
            var a = new Formula("1*a1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void DoNothingOperations2()
        {
            var a = new Formula("a1*1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void DoNothingOperations3()
        {
            var a = new Formula("0+a1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void DoNothingOperations4()
        {
            var a = new Formula("a1+0");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void DoNothingOperations5()
        {
            var a = new Formula("a1-0");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void DoNothingOperations6()
        {
            var a = new Formula("0/a1");
            a.Evaluate((_) => 1);
        }
        [TestMethod]
        public void DoNothingOperations7()
        {
            var a = new Formula("a1/1");
            a.Evaluate((_) => 1);
        }

        //unsafe
        [TestMethod]
        public void UnsafeRecipricolDivision()
        {
            var a = new Formula("a1/2");
            a.Evaluate((_) => 1);
        }

        //test equality
        [TestMethod]
        public void SpaceDotEquality()
        {
            var a = new Formula("a1/2");
            var b = new Formula("a1 / 2");
            Assert.AreEqual(true, a.Equals(b));
        }
        [TestMethod]
        public void SpaceEqualsEquality()
        {
            var a = new Formula("a1/2");
            var b = new Formula("a1 / 2");
            Assert.AreEqual(true, a == b);
        }
        [TestMethod]
        public void SpaceNotEqualsEquality()
        {
            var a = new Formula("a1/2");
            var b = new Formula("a1 / 2");
            Assert.AreEqual(false, a != b);
        }

        //get variables
        [TestMethod]
        public void GetVariables()
        {
            var a = new Formula("a1/2");
            Assert.AreEqual("a1", a.GetVariables().First());
        }

        //closing parens as imm
        [TestMethod]
        public void ClosingParensAddativeAndMult()
        {
            var a = new Formula("5*(6+4)");
            a.Evaluate((_) => 1);
        }

        //add by add
        [TestMethod]
        public void AddTwice()
        {
            var a = new Formula("5+5+5");
            a.Evaluate((_) => 1);
        }

        //syntax errors
        [ExpectedException(typeof(FormulaError))]
        public void SyntaxErrorParens0()
        {
            var a = new Formula("(");
            Assert.AreEqual(15, a.Evaluate((_) => 1));
        }

        //syntax errors
        [ExpectedException(typeof(FormulaError))]
        public void SyntaxErrorParens1()
        {
            var a = new Formula("(5+5+5");
            Assert.AreEqual(15, a.Evaluate((_) => 1));
        }

        [ExpectedException(typeof(FormulaError))]
        public void SyntaxErrorParens2()
        {
            var a = new Formula("5+5+5)");
            Assert.AreEqual(15, a.Evaluate((_) => 1));
        }

        [ExpectedException(typeof(FormulaError))]
        public void SyntaxErrorUnaryPlus()
        {
            var a = new Formula("5++5");
            Assert.AreEqual(10, a.Evaluate((_) => 1));
        }

        [ExpectedException(typeof(FormulaError))]
        public void SyntaxErrorUnaryMius()
        {
            var a = new Formula("-5");
            Assert.AreEqual(-5, a.Evaluate((_) => 1));
        }

        [ExpectedException(typeof(FormulaError))]
        public void NullVar()
        {
            var a = new Formula("a1");
            double[] d = { 5 };
            Assert.AreEqual(5, a.Evaluate((_) => d[8]));
        }

        [ExpectedException(typeof(FormulaError))]
        public void NoVar()
        {
            var a = new Formula("a1", (s) => s, (_) => false);
            Assert.AreEqual(5, a.Evaluate((_) => 5));
        }

        [ExpectedException(typeof(FormulaError))]
        public void EqualsNull()
        {
            var a = new Formula("1");
            Assert.AreEqual(true, a.Equals(null));
        }

        [ExpectedException(typeof(FormulaError))]
        public void EqualsOtherObj()
        {
            object a = new Formula("1");
            object o = "hi";
            Assert.AreEqual(true, a.Equals(o));
        }

        [ExpectedException(typeof(FormulaError))]
        public void ErrorTokens()
        {
            object a = new Formula("05.09.2001");
            Assert.AreEqual(true, a.Equals(5));
        }

        [ExpectedException(typeof(FormulaError))]
        public void ErrorToken()
        {
            object a = new Formula("$");
            Assert.AreEqual(true, a.Equals("$"));
        }

        [ExpectedException(typeof(FormulaError))]
        public void ErrorTokenMult()
        {
            object a = new Formula("*5");
            Assert.AreEqual(true, a.Equals("5"));
        }

        //fuzzing
        [TestMethod]
        public void fuzz()
        {
            Dictionary<string, int> lookupDict = new Dictionary<string, int>();
            Func<string, double> lu = (string s) => lookupDict.GetValueOrDefault(s, 0);
            Random random = new Random();
            string testCase;
            object result;
            for (int i = 0; i < 10; i++)//one thousand test cases
            {

                //should succeed
                testCase = createTestCase(lookupDict, false);
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    Console.WriteLine(string.Format("{0} = {1}", testCase, result));
                }
                catch (Exception e)
                {
                    if (e.Message != "Division by zero") Console.WriteLine(string.Format("(should be valid case) test failed for {0}", testCase));
                }



                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = testCase.Insert(random.Next(0, testCase.Length), new List<string> { "(", ")" }[random.Next(0, 1)]);
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("(should be invalid case) test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }

                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = "-" + testCase;
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }

                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = testCase + "-";
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }

                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = testCase + "+";
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }


                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = "+" + testCase;
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }

                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = testCase + "/0";
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }


                //should fail
                testCase = createTestCase(lookupDict, false);
                testCase = testCase + "/(3-3)";
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }


                //should fail
                testCase = createTestCase(lookupDict, true);
                try
                {
                    result = new Formula(testCase).Evaluate(lu);
                    System.Console.WriteLine(string.Format("test failed for {0}", testCase));
                }
                catch (Exception)
                {
                    //test successful
                }

            }
            Console.WriteLine("Done");
        }
    }
}
