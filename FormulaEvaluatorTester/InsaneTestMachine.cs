///<summary>
/// Author:    Robert Morelli
/// Partner:   None
/// Date:      1/18/2024
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and [Your Name(s)] - Do Whateva
///
/// I, Robert Morelli, certify that I wrote this code from scratch and
/// did not copy it in part or whole from code that is not my own. All 
/// references to code that is not my own used in the completion of the
/// assignments are cited in my README file.
///
/// File Contents
///     A tester for the FormulaEvaluator class. This program uses a CFG
///     to generate random formulas and then evaluates them using the
///     the evaluator.
/// </summary>

using System.Text.RegularExpressions;
using SpreadsheetTests;
using static FormulaEvaluator.Evaluator;


namespace FormulaEvaluatorTester;

internal class InsaneTestMachine
{
    /// <summary>
    ///     Execute a bunch of test cases. use generator to make valid strings and then add problems for fail cases.
    ///     Not sure if this counts as a hand-written test. I did hand write this code.
    /// </summary>
    /// <param name="_">ignore</param>
    private static void Main(string[] _)
    {
        // Create an instance of the test class
        var testClassInstance = Activator.CreateInstance(typeof(SpreadsheetTest));

        // Get methods from the test class
        var methods = typeof(SpreadsheetTest).GetMethods();

        foreach (var method in methods)
        {
            Console.WriteLine("started a test: " + method.Name);
            try
            {
                method.Invoke(testClassInstance, null);
            }
            catch
            {
            }
            finally
            {
                Console.WriteLine("did a test");
            }
        }


        var lookupDict = new Dictionary<string, int>();
        Lookup lu = s => lookupDict.GetValueOrDefault(s, 0);
        var random = new Random();
        string testCase;
        int result;
        for (var i = 0; i < 10; i++) //one thousand test cases
        {
            //should succeed
            testCase = createTestCase(lookupDict, false);
            try
            {
                result = Evaluate(testCase, lu);
                Console.WriteLine("{0} = {1}", testCase, result);
            }
            catch (Exception e)
            {
                if (e.Message != "Division by zero")
                    Console.WriteLine("(should be valid case) test failed for {0}", testCase);
            }


            //should fail
            testCase = createTestCase(lookupDict, false);
            testCase = testCase.Insert(random.Next(0, testCase.Length),
                new List<string> { "(", ")" }[random.Next(0, 1)]);
            try
            {
                result = Evaluate(testCase, lu);
                Console.WriteLine("(should be invalid case) test failed for {0}", testCase);
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
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
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
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
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
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
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
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
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
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
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
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
            }
            catch (Exception)
            {
                //test successful
            }


            //should fail
            testCase = createTestCase(lookupDict, true);
            try
            {
                result = Evaluate(testCase, lu);
                Console.WriteLine("test failed for {0}", testCase);
            }
            catch (Exception)
            {
                //test successful
            }
        }

        Console.WriteLine("Done");
    }


    /// <summary>
    ///     Generate a viable test case using a cfg
    ///     The code is pretty obvious if you know what a CFG is supposed to look like
    ///     I will not give an explanation of a CFG here
    /// </summary>
    /// <param name="lookupDict">
    ///     a dictionary to be passed to the evaluator.
    /// </param>
    /// <param name="errorVar">
    ///     bool to add nonexistent var to fromula
    /// </param>
    /// <returns></returns>
    private static string createTestCase(Dictionary<string, int> lookupDict, bool errorVar)
    {
        var random = new Random();
        var expansions = 1000;
        var CFG = new Dictionary<string, List<GetAChild>>
        {
            { "&S", new List<GetAChild> { () => "&E" } },
            {
                "&E", new List<GetAChild>
                {
                    () => "&T",
                    () => "(&W&T&W)",
                    () => "&T&W&O&W&T",
                    () =>
                    {
                        expansions--;
                        return "&E&W&O&W&T";
                    },
                    () =>
                    {
                        expansions--;
                        return "&T&W&O&W&E";
                    },
                    () =>
                    {
                        expansions--;
                        return "(&W&E&W)";
                    },
                    () =>
                    {
                        expansions -= 2;
                        return "&E&W&O&W&E";
                    },
                    () =>
                    {
                        expansions -= 2;
                        return "&E&W&O&W&E";
                    },
                    () =>
                    {
                        expansions -= 2;
                        return "&E&W&O&W&E";
                    }
                }
            },
            {
                "&T", new List<GetAChild>
                {
                    () => random.Next(1, 1000).ToString(),
                    () =>
                    {
                        var key = GenerateRandomAlphabeticString(random.Next(1, 4)) + random.Next(1, 1000);
                        if (lookupDict.ContainsKey(key)) return key;

                        var val = random.Next(1, 1000);
                        lookupDict.Add(key, val);
                        return key;
                    }
                }
            },
            {
                "&O", new List<GetAChild>
                {
                    () => "+",
                    () => "-",
                    () => "/",
                    () => "*"
                }
            },

            {
                "&W", new List<GetAChild>
                {
                    () =>
                    {
                        var space = "";
                        for (var i = 0; i < random.Next(0, 4); i++) space += " ";
                        return space;
                    }
                }
            }
        };

        var exp = "&S";
        var tokenIdentifier = new Regex("(&[WSTOE])");
        while (exp.Contains("&"))
        {
            var tokens = Regex.Split(exp, tokenIdentifier.ToString());
            exp = "";
            foreach (var token in tokens)
                if (tokenIdentifier.IsMatch(token))
                {
                    if (token == "&T" && errorVar)
                    {
                        exp += " a0 ";
                        errorVar = false;
                    }

                    var possibilities = CFG.GetValueOrDefault(token, [() => "1"]);
                    if (expansions < 0)
                        exp += possibilities[0].Invoke();
                    else
                        exp += possibilities[random.Next(0, possibilities.Count())].Invoke();
                }
                else
                {
                    exp += token;
                }
        }

        return exp;
    }

    /// <summary>
    ///     makes random alphabetic strings via the most niavest algorithm
    /// </summary>
    /// <param name="length">
    ///     how long should it be
    /// </param>
    /// <returns>
    ///     the random string
    /// </returns>
    private static string GenerateRandomAlphabeticString(int length)
    {
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var random = new Random();
        var randomChars = new char[length];
        for (var i = 0; i < length; i++)
        {
            var randomIndex = random.Next(0, alphabet.Length);
            randomChars[i] = alphabet[randomIndex];
        }

        return new string(randomChars);
    }

    private delegate string GetAChild();
}