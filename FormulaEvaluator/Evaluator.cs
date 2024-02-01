

using System.Text.RegularExpressions;

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
///     This is a formula evaluator that takes in a string and evaluates
///     it. In the larger context this is intended to be used to evaluate
///     formulas in a spreadsheet. The evaluator takes in a string and
///     parses/evaluates it using the provided algorithm.
///     
/// </summary>


namespace FormulaEvaluator
{
    /// <summary>
    /// Class to hold our one useful function
    /// </summary>
    public partial class Evaluator
    {
        private delegate void TokenProcFunc(string token);
        public delegate int Lookup(String variable_name);

        /// <summary>
        /// Evaluates a math expression for a spreadsheet
        /// </summary>
        /// <param name="expression">
        /// The expression to be evaluated
        /// </param>
        /// <param name="variableEvaluator">
        /// A lookup function for variables present in the formula
        /// </param>
        /// <returns>
        /// The value of the expression as an integer
        /// </returns>
        /// <exception cref="Exception">
        /// Errors come from malformed expressions and missing variables
        /// </exception>
        public static int Evaluate(String expression,
                                   Lookup variableEvaluator)
        {
            var valueStack = new Stack<int>();
            var operatorStack = new Stack<string>();
            //dictionary of correct action items
            var tokenProcessor = generateTokenProcessorDictionary(valueStack, operatorStack, variableEvaluator);
            expression = string.Format("({0})*1", expression); //gaurentees one value at end (removes some checking)
            Regex
                .Split(expression, findTokenRegex().ToString()) //spllit expression into tokens
                .ToList().ForEach((token) =>
                    //for each token determine the correct action to take (or error action)
                    tokenProcessor
                        .AsEnumerable()
                        .FirstOrDefault(
                            (keyValuePair) =>
                                keyValuePair.Key.IsMatch(token)
                            , errorTokenAction)
                        //do the action that was determined appropriate for the token
                        .Value.Invoke(token)
                     );
            //either add/sub the last two, return the value, or fail.
            if (operatorStack.Count != 0) throw new ArgumentException("Malformed Expression");
            else if (valueStack.Count != 1) throw new ArgumentException("Too many or too few values for amount of operators");
            else return valueStack.Pop();
        }


        //the value, the whole value, and nothing but the value -> "^...$"
        //all the required matching. obv some of these could be direct string comp
        //but its fine cuz uniformity
        //each one of these identifies some subset of potention tokens


        private readonly static TokenProcFunc nothingFunc = (token) => { };
        private readonly static TokenProcFunc everythingFunc = (token) =>
        {
            throw new ArgumentException(string.Format("Could not identify token ({0})", token));
        };

        private static KeyValuePair<Regex, TokenProcFunc> errorTokenAction =
                                                         new KeyValuePair<Regex, TokenProcFunc>(isAnythingRegex(), everythingFunc);


        /// <summary>
        /// Generates a dictionary where the keys are regex that recognize the right thing to be parsed
        /// and the values are delegates of what to do.
        /// 
        /// Built this way to be composable with other dictionaries such that other tokens may be recognised.
        /// Should probably use ordered dictionary so theres no lookup cost but its minimal and not N/big O relevant
        /// This could be made better if i do a global variable replace and then a direct dictionary lookup
        /// </summary>
        /// <param name="valueStack">
        /// The value stack used by the delegates to push values.
        /// </param>
        /// <param name="operatorStack">
        /// The operator stack used by the delegates
        /// </param>
        /// <param name="variableEvaluator">
        /// The variable lookup used by the variable delegate
        /// </param>
        /// <returns>
        /// A dictionary with keys and values for appropriate operations to perform per regex
        /// </returns>
        /// <exception cref="Exception">
        /// This function will not throw errors, the returned delegates will throw errors
        /// for improper formulas
        /// </exception>
        private static Dictionary<Regex, TokenProcFunc> generateTokenProcessorDictionary(Stack<int> valueStack, Stack<string> operatorStack, Lookup variableEvaluator)
        {

            /// <summary>
            /// If * or / is at the top of the operator stack, pop the value stack,
            /// pop the operator stack, and apply the popped operator to the popped
            /// number and t. Push the result onto the value stack.
            /// 
            /// Otherwise, push t onto the value stack.
            /// </summary>
            void intFunc(string token)
            {
                if (operatorStack.Count > 0 && isMultiplicativeRegex().IsMatch(operatorStack.Peek()))
                    if (valueStack.Count == 0) throw new ArgumentException("Infix operator only found one operand");
                    else if (isZeros().IsMatch(token)) throw new ArgumentException("Division by zero");
                    else if (isMultRegex().IsMatch(operatorStack.Pop()))
                        valueStack.Push(valueStack.Pop() * int.Parse(token));
                    else
                        valueStack.Push(valueStack.Pop() / int.Parse(token));
                else

                    valueStack.Push(int.Parse(token));

            }

            /// <summary>
            /// Proceed as above, using the looked-up value of t instead of t
            /// </summary>
            void varFunc(string token)
            {
                int? val;
                try
                {
                    val = variableEvaluator(token);
                    if (val == null) throw new ArgumentException("your variable sucks");
                }
                catch (Exception)
                {
                    //rethrow same error in some cases. cry about it
                    throw new ArgumentException("your variable evaluator sucks");
                }
                //zero never happens. idk how c# nullable promotion works. maybe try crying and ill change it
                intFunc(val.ToString() ?? "0");

            }
            /// <summary>
            /// "If + or - is at the top of the operator stack,
            /// pop the value stack twice and the operator stack once,
            /// then apply the popped operator to the popped numbers,
            /// then push the result onto the value stack.
            /// 
            /// Push t onto the operator stack"
            /// </summary>
            void addativeFunc(string token)
            {
                if (operatorStack.Count > 0 && isAddativeRegex().IsMatch(operatorStack.Peek()))
                    if (valueStack.Count < 2) throw new ArgumentException("Two adds in a row");
                    else if (isAddRegex().IsMatch(operatorStack.Pop()))
                        valueStack.Push(valueStack.Pop() + valueStack.Pop());
                    else
                        valueStack.Push(valueStack.Pop() - valueStack.Pop());
                operatorStack.Push(token);
            }


            /// <summary>
            /// Push t onto the operator stack
            /// 
            /// also unnecessary func wrapper. try crying if you dont like it
            /// </summary>
            void multiplicativefunc(string token) => operatorStack.Push(token);


            /// <summary>
            /// Push t onto the operator stack
            /// 
            /// duplicate of mult. cry about it
            /// </summary>
            void openParenFunc(string token) => operatorStack.Push(token);

            /// <summary>
            /// Do all three of these steps in order:
            /// (1) 
            ///     If + or - is at the top of the operator stack,
            ///     pop the value stack twice and the operator stack
            ///     once. Apply the popped operator to the popped
            ///     numbers. Push the result onto the value stack.
            /// (2)
            ///     The top of the operator stack should be a '('.Pop it.
            /// (3)
            ///     If* or / is at the top of the operator stack, pop the
            ///     value stack twice and the operator stack once. Apply
            ///     the popped operator to the popped numbers. Push the
            ///     result onto the value stack.
            /// </summary>
            void closeParenFunc(string token)
            {
                if (operatorStack.Count > 1 && isAddativeRegex().IsMatch(operatorStack.Peek()))
                    if (valueStack.Count < 2) throw new ArgumentException("Not enough items to add within parenthesis");
                    else if (isAddRegex().IsMatch(operatorStack.Pop()))
                        valueStack.Push(valueStack.Pop() + valueStack.Pop());
                    else
                        valueStack.Push(valueStack.Pop() - valueStack.Pop());
                if (operatorStack.Count == 0 || !isOpeningParenRegex().IsMatch(operatorStack.Pop()))
                    throw new ArgumentException("Unmatched closing parenthesis");

                if (operatorStack.Count > 0 && isMultiplicativeRegex().IsMatch(operatorStack.Peek()))
                    intFunc(valueStack.Pop().ToString());
            }

            // dictionary of regex matched with correct response.
            return new Dictionary<Regex, TokenProcFunc> {
                {isWhiteSpaceRegex(), nothingFunc},
                {isIntRegex(), intFunc },
                {isVariableRegex(), varFunc },
                {isAddativeRegex(), addativeFunc },
                {isMultiplicativeRegex(), multiplicativefunc },
                {isOpeningParenRegex(), openParenFunc },
                {isClosingParenRegex(), closeParenFunc },
            };

        }


        [GeneratedRegex("\\s*(\\(|\\)|-|\\+|\\*|/)\\s*")] private static partial Regex findTokenRegex();
        [GeneratedRegex("^(\\s*)$")] private static partial Regex isWhiteSpaceRegex();
        [GeneratedRegex("^(\\d+)$")] private static partial Regex isIntRegex();
        [GeneratedRegex("^(\\w+\\d+)$")] private static partial Regex isVariableRegex();
        [GeneratedRegex("^(\\+|-$)")] private static partial Regex isAddativeRegex();
        [GeneratedRegex("^(\\+)$")] private static partial Regex isAddRegex();
        [GeneratedRegex("^(\\*|/)$")] private static partial Regex isMultiplicativeRegex();
        [GeneratedRegex("^(\\*)$")] private static partial Regex isMultRegex();
        [GeneratedRegex("^(\\()$")] private static partial Regex isOpeningParenRegex();
        [GeneratedRegex("^(\\))$")] private static partial Regex isClosingParenRegex();
        [GeneratedRegex("^(.*)$")] private static partial Regex isAnythingRegex();
        [GeneratedRegex("^(0+)$")] private static partial Regex isZeros();
    }
}
