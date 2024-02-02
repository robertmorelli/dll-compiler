///<summary>
/// written exclusively by Robert Morelli
/// 
/// please note: this generates an AST which was
/// not required by the assignment but seemed like fun
/// if you want to know why tf I wrote this
/// probably look at what an AST is:
/// https://en.wikipedia.org/wiki/Abstract_syntax_tree
/// 
/// I did ask the prof and he said I was allowed to do this
/// </summary>


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{

    public static partial class Utility
    {
        public static void test()
        {
            // Example usage
            var f = new Formula("(4+5+6)-2/2/0");
            Console.WriteLine("---");
            Console.WriteLine(f.Evaluate((_) => 1));
            Console.WriteLine("---");

        }
        // Patterns for individual tokens
        const string lpPattern = @"\(";
        const string rpPattern = @"\)";
        const string opPattern = @"[\+\-*/]";
        const string varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        const string doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
        const string spacePattern = @"\s+";
        // Overall pattern
        const string tokenPattern = @"("
                                    + lpPattern + @"|"
                                    + rpPattern + @"|"
                                    + opPattern + @"|"
                                    + varPattern + @"|"
                                    + doublePattern + @"|"
                                    + spacePattern +
                                    @")";
        const string whiteLinePattern = @"^(\s*)$";
        const string intPattern = @"^(\d+)$";
        const string addativePattern = @"^(\+|-$)";
        const string addPattern = @"^(\+)$";
        const string subPattern = @"^(-)$";
        const string multiplicativePattern = @"^(\*|/)$";
        const string multPattern = @"^(\*)$";
        const string divPattern = @"^(/)$";
        const string anyOneLinePattern = @"^(.*)$";
        const string isZerosPattern = @"^(0+)$";
        const string doubleAlonePattern = @"^(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?$";

        //speed up regex performance
        //no backtracking makes this a DFA instead of NFA
        //GeneratedRegex makes these all compiled at compile time
        //(as opposed to run time)
        const RegexOptions ro =
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking;

        [GeneratedRegex(doubleAlonePattern, options: ro)] public static partial Regex isDouble();
        [GeneratedRegex(opPattern, options: ro)] public static partial Regex isOperation();
        [GeneratedRegex(tokenPattern, options: ro)] public static partial Regex findTokenRegex();
        [GeneratedRegex(whiteLinePattern, options: ro)] public static partial Regex isWhiteSpaceRegex();
        [GeneratedRegex(intPattern, options: ro)] public static partial Regex isIntRegex();
        [GeneratedRegex(varPattern, options: ro)] public static partial Regex isVariableRegex();
        [GeneratedRegex(addativePattern, options: ro)] public static partial Regex isAddativeRegex();
        [GeneratedRegex(addPattern, options: ro)] public static partial Regex isAddRegex();
        [GeneratedRegex(subPattern, options: ro)] public static partial Regex isSubRegex();
        [GeneratedRegex(multiplicativePattern, options: ro)] public static partial Regex isMultiplicativeRegex();
        [GeneratedRegex(multPattern, options: ro)] public static partial Regex isMultRegex();
        [GeneratedRegex(divPattern, options: ro)] public static partial Regex isDivRegex();
        [GeneratedRegex(lpPattern, options: ro)] public static partial Regex isOpeningParenRegex();
        [GeneratedRegex(rpPattern, options: ro)] public static partial Regex isClosingParenRegex();
        [GeneratedRegex(anyOneLinePattern, options: ro)] public static partial Regex isAnythingRegex();
        [GeneratedRegex(isZerosPattern, options: ro)] public static partial Regex isZeros();
    }

    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(string formula) :
            this(formula, s => s, s => true)
        {
        }

        private List<string> normalTokens;
        private List<Token> tokens;
        private TokenNode ExecutableAst;

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(string formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            var valueStack = new Stack<TokenNode>();
            var operatorStack = new Stack<Token>();
            //dictionary of correct action items
            var tokenProcessor = generateTokenProcessorDictionary(valueStack, operatorStack, (e) => 1);
            tokens = GetNormalTokens(formula, normalize, isValid).ToList();
            normalTokens = tokens.Select((token) => token.primativeString).ToList();
            foreach (var token in multiplyBy1(tokens))
                if (tokenProcessor.TryGetValue(token.Type, out var procFunc))
                    procFunc(token);
            ExecutableAst = valueStack.Pop().Optmizied();
        }

        private static IEnumerable<Token> multiplyBy1(IEnumerable<Token> tokenList)
        {
            yield return new Token("(");
            foreach (var token in tokenList) yield return token;
            yield return new Token(")");
            yield return new Token("*");
            yield return new Token("1");
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            try
            {
                return ExecutableAst.Value(lookup);
            } catch (Exception)
            {
                return new FormulaError();
            }
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<string> GetVariables()
        {
            return tokens
                .FindAll((token) => token.isVar)
                .Select((token) => token.primativeString )
                .AsEnumerable();
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            return normalTokens.Aggregate("",(a,b) => a + b);
        }

        /// <summary>
        ///  <change> make object nullable </change>
        ///
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object? obj)
        {
            return false;
        }

        /// <summary>
        ///   <change> We are now using Non-Nullable objects.  Thus neither f1 nor f2 can be null!</change>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// 
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            return false;
        }

        /// <summary>
        ///   <change> We are now using Non-Nullable objects.  Thus neither f1 nor f2 can be null!</change>
        ///   <change> Note: != should almost always be not ==, if you get my meaning </change>
        ///   Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            return false;
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return 0;
        }

        //types of tokens for the parse algorithm
        internal enum TokenType
        {
            multiplicative = 0,
            additive = 1,
            var = 2,
            val = 3,
            openParen = 4,
            closedParen = 5,
            erroneous = 6,
        }

        /// <summary>
        /// element to build AST out of
        /// elements with primary ops are to be evaluated
        /// elements with primary vals are either immediate value or var
        /// </summary>
        internal struct TokenNode
        {
            //non-optimizing constructor
            public TokenNode(Token primary, TokenNode? left, TokenNode? right)
            {
                this.primary = primary;
                _leftRef = new(left);
                _rightRef = new(right);
                constValue = primary.isImm ?
                double.TryParse(primary.primativeString, out double d) ?
                    d :
                    double.NaN :
                double.NaN;
                if (LeftChild != null && RightChild != null)
                {
                    var realLeft = (TokenNode)LeftChild;
                    var realRight = (TokenNode)RightChild;
                    //NaN o K = NaN unless NaN * 0 in some cases does accidental optimizations to remove vars
                    constValue = primary.primativeString switch
                    {
                        "*" => realLeft.constValue * realRight.constValue,
                        "/" => realLeft.constValue / realRight.constValue,
                        "+" => realLeft.constValue + realRight.constValue,
                        "-" => realLeft.constValue - realRight.constValue,
                        _ => double.NaN,
                    };
                }
            }

            //optimizes AST tree and returns best version this code can produce
            public readonly TokenNode Optmizied(bool unsafeOptimizations = false)
            {
                if (primary.IsValue) return this;
                if (HasConstValue) return new TokenNode(new Token(constValue.ToString()), null, null);
                if (LeftChild != null && RightChild != null)
                {
                    var realLeft = ((TokenNode)LeftChild).Optmizied(unsafeOptimizations);
                    var realRight = ((TokenNode)RightChild).Optmizied(unsafeOptimizations);
                    //pre-evaluate k1 o k2 = k3
                    if (realLeft.HasConstValue && realRight.HasConstValue)
                    {
                        return
                            new TokenNode(
                            new Token((primary.primativeString switch
                            {
                                "*" => realLeft.constValue * realRight.constValue,
                                "/" => realLeft.constValue / realRight.constValue,
                                "+" => realLeft.constValue + realRight.constValue,
                                "-" => realLeft.constValue - realRight.constValue,
                                _ => double.NaN,
                            }).ToString()), null, null);
                    }
                    //clip branches with identity operations % * 1, % / 1, % + 0, % - 0
                    if (primary.IsAddative)
                    {
                        if (realLeft.constValue.Equals(0)) return realRight;
                        if (realRight.constValue.Equals(0)) return realLeft;
                    }
                    else if (primary.IsMultiplicative)
                    {
                        if (realLeft.constValue.Equals(1)) return realRight;
                        if (realRight.constValue.Equals(1)) return realLeft;
                    }
                    if (unsafeOptimizations)
                    {
                        //recipricol const division % / k1, k2 = 1/k1, do % * k2
                        if (primary.isDiv && realRight.HasConstValue) return new TokenNode(
                                new Token("*"),
                                realLeft,
                                new TokenNode(new Token((1 / realRight.constValue).ToString()), null, null)
                                );
                        // TODO: tree balancing ((%1 * %2) * %3) * %4 = (%1 * %2) * (%3 * %4)
                        // btw technically not valid for float types

                        // TODO: %1 + %1 = 2 * %1
                        // TODO: (k1 * %1) + %1, k2 = k1 + 1, do k2 * %
                    }
                }
                return this;
            }

            //does a preorder traversal lisp-y string for debugging "k" indicates calculated constant vals
            public readonly string ToString(Func<string, double>? lo = null, int depth = 0)
            {
                lo ??= (_) => 1;
                return
                    "\n" + new string(' ', depth * 4) +
                    (LeftChild != null ? "(" : "") +
                    primary.primativeString.ToString() +
                    (HasConstValue ? " K" + (LeftChild != null ? " :" + constValue : "") : "") +
                    LeftChild?.ToString(lo, depth + 1) +
                    RightChild?.ToString(lo, depth + 1) +
                    (LeftChild != null ? ("\n" + new string(' ', depth * 4) + ")") : "");
            }

            //who said there were no pointers in "safe" c#?
            internal class ReferenceLMAO(TokenNode? tn) { public TokenNode? tn = tn; }
            private readonly ReferenceLMAO _leftRef;
            private readonly ReferenceLMAO _rightRef;
            //ease of use get from TokenNode*
            public readonly TokenNode? LeftChild { get => _leftRef.tn; }
            public readonly TokenNode? RightChild { get => _rightRef.tn; }

            //primary token for this node
            public readonly Token primary;

            //NaN indicates var dependance. sue me. cry about it. shout at the sky even
            public readonly bool HasConstValue { get => !double.IsNaN(constValue); }

            //for optimizing
            public double constValue;

            //should never really produce NaNs. I thing 10/(1/1E-25) or something might trigger this
            //you would have to divide by infinite
            public readonly double Value(Func<string, double> lookup)
            {
                if (HasConstValue) return constValue;
                if (primary.isVar) return lookup(primary.primativeString);
                if (LeftChild == null || RightChild == null) return double.NaN;
                var leftValue = LeftChild?.Value(lookup) ?? double.NaN;
                var rightValue = RightChild?.Value(lookup) ?? double.NaN;
                return primary.primativeString switch
                {
                    "*" => leftValue * rightValue,
                    "/" => leftValue / rightValue,
                    "+" => leftValue + rightValue,
                    "-" => leftValue - rightValue,
                    _ => double.NaN,
                };
            }
        }

        //represents a token
        //should probably be built with an enum
        // TODO: build with enum
        internal struct Token
        {
            public bool IsOperation { get => IsAddative || IsMultiplicative || IsParens; }
            public readonly bool IsAddative { get => isAdd || isSub; }
            public bool isAdd = false;
            public bool isSub = false;
            public readonly bool IsMultiplicative { get => isMult || isDiv; }
            public bool isMult = false;
            public bool isDiv = false;
            public bool IsValue { get => isImm || isVar; }
            public bool isImm = false;
            public bool isVar = false;
            public bool IsParens { get => isLParens || isRParens; }
            public bool isLParens = false;
            public bool isRParens = false;
            public string primativeString;
            public Token(string primative)
            {
                if (isImm = double.TryParse(primative, out double d)) primativeString = d.ToString();
                else primativeString = primative;
                isVar = Utility.isVariableRegex().IsMatch(primative);
                isDiv = Utility.isDivRegex().IsMatch(primative);
                isMult = Utility.isMultRegex().IsMatch(primative);
                isAdd = Utility.isAddRegex().IsMatch(primative);
                isSub = Utility.isSubRegex().IsMatch(primative);
                isLParens = Utility.isOpeningParenRegex().IsMatch(primative);
                isRParens = Utility.isClosingParenRegex().IsMatch(primative);
            }
            public readonly TokenType Type
            {
                get
                {
                    if (IsAddative) return TokenType.additive;
                    if (IsMultiplicative) return TokenType.multiplicative;
                    if (isVar) return TokenType.var;
                    if (isLParens) return TokenType.openParen;
                    if (isRParens) return TokenType.closedParen;
                    if (isImm) return TokenType.val;
                    return TokenType.erroneous;
                }
            }
        }

        private delegate void TokenProcFunc(Token token);

        

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<Token> GetNormalTokens(string formulaPrimative, Func<string, string> normalize, Func<string, bool> isValid)
        {
            string formula = formulaPrimative;
            foreach (var s in Utility.findTokenRegex().Split(formula))
                if (!Utility.isWhiteSpaceRegex().IsMatch(s))
                {
                    Token token;
                    if (double.TryParse(s, out double d)) token = new Token(d.ToString());
                    else token = new Token(s.Trim());
                    if (token.isVar) token = new Token(normalize(token.primativeString));
                    if (!isValid(token.primativeString)) throw new FormulaFormatException("die exception");
                    yield return token;
                }
        }

        public delegate int Lookup(string variable_name);

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
        private static Dictionary<TokenType, TokenProcFunc> generateTokenProcessorDictionary(Stack<TokenNode> valueStack, Stack<Token> operatorStack, Lookup variableEvaluator)
        {
            /// <summary>
            /// If * or / is at the top of the operator stack, pop the value stack,
            /// pop the operator stack, and apply the popped operator to the popped
            /// number and t. Push the result onto the value stack.
            /// 
            /// Otherwise, push t onto the value stack.
            /// </summary>
            void immFunc(Token token)
            {
                if (operatorStack.Count != 0 && operatorStack.Peek().IsMultiplicative)
                {

                    if (valueStack.Count == 0) throw new ArgumentException("Infix operator only found one operand");
                    if (operatorStack.Peek().isDiv)
                        if (double.TryParse(token.primativeString, out double d))
                            if (d == 0.0)
                                throw new ArgumentException("divideByZero");
                    valueStack.Push(new TokenNode(
                        operatorStack.Pop(),
                        valueStack.Pop(),
                        new TokenNode(token, null, null)
                        ));

                } 
                else
                {
                    valueStack.Push(new TokenNode(token, null, null));
                }
            }

            /// <summary>
            /// Proceed as above, using the looked-up value of t instead of t
            /// </summary>
            void varFunc(Token token)
            {
                immFunc(token);
            }
            /// <summary>
            /// "If + or - is at the top of the operator stack,
            /// pop the value stack twice and the operator stack once,
            /// then apply the popped operator to the popped numbers,
            /// then push the result onto the value stack.
            /// 
            /// Push t onto the operator stack"
            /// </summary>
            void addativeFunc(Token token)
            {
                if (operatorStack.Peek().IsAddative)
                {
                    if (valueStack.Count < 2) throw new ArgumentException("adding just one");
                    valueStack.Push(
                        new TokenNode(
                            operatorStack.Pop(),
                            right: valueStack.Pop(),
                            left: valueStack.Pop()
                            )
                        );
                }
                operatorStack.Push(token);
            }


            /// <summary>
            /// Push t onto the operator stack
            /// 
            /// also unnecessary func wrapper. try crying if you dont like it
            /// </summary>
            void multiplicativefunc(Token token) => operatorStack.Push(token);


            /// <summary>
            /// Push t onto the operator stack
            /// 
            /// duplicate of mult. cry about it
            /// </summary>
            void openParenFunc(Token token) => operatorStack.Push(token);

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
            void closeParenFunc(Token token)
            {
                if (operatorStack.Count == 0) throw new ArgumentException("Unmatched closing parenthesis");
                if (operatorStack.Count > 1)
                    if (operatorStack.Peek().IsAddative)
                        if (valueStack.Count < 2) throw new ArgumentException("unary add within parenthesis");
                        else valueStack.Push(
                            new TokenNode(
                                primary: operatorStack.Pop(),
                                right: valueStack.Pop(),
                                left: valueStack.Pop()
                                )
                        );
                if (operatorStack.Count == 0) throw new ArgumentException("Unmatched closing parenthesis");
                if (!operatorStack.Pop().isLParens) throw new ArgumentException("Unmatched closing parenthesis");
                if (operatorStack.Count == 0) return;
                if (!operatorStack.Peek().IsMultiplicative) return;
                if (valueStack.Count < 2) throw new ArgumentException();
                if (valueStack.Peek().primary.isImm)
                    if (double.TryParse(valueStack.Peek().primary.primativeString, out double d))
                        if (d == 0.0) throw new ArgumentException();

            }

            // dictionary of regex matched with correct response.
            return new Dictionary<TokenType, TokenProcFunc> {
                {TokenType.val, immFunc },
                {TokenType.var, varFunc },
                {TokenType.additive, addativeFunc },
                {TokenType.multiplicative, multiplicativefunc },
                {TokenType.openParen, openParenFunc },
                {TokenType.closedParen, closeParenFunc },
            };

        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(string reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }
}


// <change>
//   If you are using Extension methods to deal with common stack operations (e.g., checking for
//   an empty stack before peeking) you will find that the Non-Nullable checking is "biting" you.
//
//   To fix this, you have to use a little special syntax like the following:
//
//       public static bool OnTop<T>(this Stack<T> stack, T element1, T element2) where T : notnull
//
//   Notice that the "where T : notnull" tells the compiler that the Stack can contain any object
//   as long as it doesn't allow nulls!
// </change>
