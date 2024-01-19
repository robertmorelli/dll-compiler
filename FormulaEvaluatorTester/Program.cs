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




using FormulaEvaluator;
using System.Text.RegularExpressions;


namespace FormulaEvaluatorTester
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine(Evaluator.Evaluate("1+1",(s) => 20));
        }
    }
}