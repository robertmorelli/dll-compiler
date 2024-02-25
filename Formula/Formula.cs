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
/// please note: this generates an AST which was
/// not required by the assignment but seemed like fun
/// if you want to know why tf I wrote this
/// probably look at what an AST is:
/// https://en.wikipedia.org/wiki/Abstract_syntax_tree
/// I did ask the prof and he said I was allowed to do this
/// </summary>

using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{

    public static partial class Utility
    {

        //speed up regex performance
        //no backtracking makes this a DFA instead of NFA
        //GeneratedRegex makes these all compiled at compile time
        //(as opposed to run time)
        const RegexOptions ro =
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking;

        [GeneratedRegex(@"^\s*$", options: ro)] public static partial Regex isWhiteSpaceRegex();
        [GeneratedRegex(@"^[a-zA-Z][a-zA-Z\d]*$", options: ro)] public static partial Regex isVariableRegex();
        [GeneratedRegex(@"\b|([\-)*(+/])", options: ro)] public static partial Regex bounderies();
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

        private readonly Token[] Tokens;
        private readonly string[] VarTokens;
        private TokenNode ExecutableAst;
        private readonly bool ThrowDBZEveryTime = false;
        private int HashCodeCash = 0;
        private bool HasHashCode = false;

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
            var tokenProcessor = generateTokenProcessorDictionary(valueStack, operatorStack, isValid);
            Tokens = GetNormalTokens(formula, normalize, isValid).ToArray();
            if (Tokens.Length == 0) throw new FormulaFormatException("nothin");

            tokenProcessor[TokenType.openParen](new Token("("));
            foreach (var token in Tokens) tokenProcessor[token.Type](token);
            tokenProcessor[TokenType.closedParen](new Token("("));
            tokenProcessor[TokenType.multiplicative](new Token("*"));
            tokenProcessor[TokenType.val](new Token("1"));

            ExecutableAst = valueStack.Pop();
            if ((valueStack.Count > 0) || (operatorStack.Count > 0)) throw new FormulaFormatException("unmatched parenthesis or other operator");
            ThrowDBZEveryTime = ExecutableAst.IsDBZConst();
            ExecutableAst = ExecutableAst.Optmizied();
            VarTokens = Tokens
                .Where((token) => token.isVar)
                .Select((token) => token.primativeString)
                .Distinct()
                .ToArray();
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
            if (ThrowDBZEveryTime) return new FormulaError("divide by zero for string: " + ToString());
            try { return ExecutableAst.Value(lookup); }
            catch (Exception e) { return new FormulaError(ToString() + " : " + e.Message); }
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
        public IEnumerable<string> GetVariables() => VarTokens;

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
        public override string ToString() =>
            Tokens
                .Select((token) => token.primativeString)
                .Aggregate("", (a, b) => a + (a.Length > 0 ? " " : "") + b);

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
        public override bool Equals(object? obj) =>
            obj != null &&
            (obj.GetType() == typeof(Formula)) &&
            GetHashCode() == obj.GetHashCode();


        /// <summary>
        ///   <change> We are now using Non-Nullable objects.  Thus neither f1 nor f2 can be null!</change>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// 
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2) => f1.Equals(f2);

        /// <summary>
        ///   <change> We are now using Non-Nullable objects.  Thus neither f1 nor f2 can be null!</change>
        ///   <change> Note: != should almost always be not ==, if you get my meaning </change>
        ///   Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2) => !(f1 == f2);

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            if (!HasHashCode)
            {
                HashCodeCash = ToString().GetHashCode();
                HasHashCode = true;
            }
            return HashCodeCash;
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
                constValue = primary.isImm ? double.Parse(primary.primativeString) : double.NaN;
                if (LeftChild != null)
                    if (RightChild != null)
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
                            _ => double.NaN
                        };
                    }
            }

            //optimizes AST tree and returns best version this code can produce
            public readonly TokenNode Optmizied(bool unsafeOptimizations = true)
            {
                if (primary.IsValue) return this;
                if (HasConstValue) return new TokenNode(new Token(constValue.ToString()), null, null);
                if (LeftChild != null && RightChild != null)
                {
                    var realLeft = ((TokenNode)LeftChild).Optmizied(unsafeOptimizations);
                    var realRight = ((TokenNode)RightChild).Optmizied(unsafeOptimizations);
                    if (primary.isAdd)
                    {
                        // 0 + % = %
                        if (realLeft.constValue.Equals(0)) return realRight;
                        // % + 0 = %
                        if (realRight.constValue.Equals(0)) return realLeft;
                    }
                    if (primary.isMult)
                    {
                        // 1 * % = %
                        if (realLeft.constValue.Equals(1)) return realRight;
                        // % * 1 = % 
                        if (realRight.constValue.Equals(1)) return realLeft;
                    }
                    if (primary.isSub)
                    {
                        // % - 0 = %
                        if (realRight.constValue.Equals(0)) return realLeft;
                    }
                    if (primary.isDiv)
                    {
                        // % / 1 = %
                        if (realRight.constValue.Equals(1)) return realLeft;
                    }
                    if (unsafeOptimizations)
                    {
                        //recipricol const division % / k1, k2 = 1/k1, do % * k2
                        if (primary.isDiv && realRight.HasConstValue)
                        {
                            return new TokenNode(
                                new Token("*"),
                                realLeft,
                                new TokenNode(
                                    new Token(
                                        (1 / realRight.constValue).ToString()),
                                        null,
                                        null
                                ));
                        }
                        // TODO: tree balancing ((%1 * %2) * %3) * %4 = (%1 * %2) * (%3 * %4)
                        // btw technically not valid for float types

                        // TODO: %1 + %1 = 2 * %1
                        // TODO: (k1 * %1) + %1, k2 = k1 + 1, do k2 * %
                    }

                }
                return this;
            }

            public readonly bool IsDBZConst()
            {
                var rightMaybe = RightChild;
                var leftMaybe = LeftChild;
                if (rightMaybe == null || leftMaybe == null) return false;
                var right = (TokenNode)rightMaybe;
                var left = (TokenNode)leftMaybe;
                if (right.IsDBZConst() || left.IsDBZConst()) return true;
                return primary.isDiv && right.HasConstValue && right.constValue.Equals(0);
            }

            //does a preorder traversal lisp-y string for debugging "k" indicates calculated constant vals
            public readonly string ToString2(Func<string, double>? lo = null, int depth = 0)
            {
                lo ??= (_) => 1;
                return
                    "\n" + new string(' ', depth * 4) +
                    (LeftChild != null ? "(" : "") +
                    primary.primativeString.ToString() +
                    (HasConstValue ? " K" + (LeftChild != null ? " :" + constValue : "") : "") +
                    LeftChild?.ToString2(lo, depth + 1) +
                    RightChild?.ToString2(lo, depth + 1) +
                    (LeftChild != null ? ("\n" + new string(' ', depth * 4) + ")") : "");
            }

            public readonly string ToString(Func<string, double>? lo = null)
            {
                lo ??= (_) => 1;
                return
                    (LeftChild != null ? "(" : "") +
                    LeftChild?.ToString(lo) +
                    primary.primativeString.ToString() +
                    RightChild?.ToString(lo) +
                    (LeftChild != null ? ")" : "");// +
                                                   //(LeftChild != null ? (HasConstValue ? "=" + constValue : "") : "");
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
                var leftValue = ((TokenNode)LeftChild).Value(lookup);
                var rightValue = ((TokenNode)RightChild).Value(lookup);
                if (rightValue.Equals(0) && primary.isDiv) throw new ArgumentException("divide by zero");
                return primary.primativeString switch
                {
                    "*" =>
                        leftValue * rightValue,
                    "/" =>
                        leftValue / rightValue,
                    "+" =>
                        leftValue + rightValue,
                    "-" =>
                        leftValue - rightValue,
                    _ => double.NaN //cant happen
                };
            }
        }

        /// <summary>
        /// represents a token
        /// should probably be built with an enum
        /// TODO: build with enum
        /// </summary>
        internal struct Token
        {
            /// <summary>
            /// these vars exist because im too lazy to make
            /// an enum for this. anyway this is pretty self explanitory
            /// this is all token types with some inheritance
            /// </summary>
            //public bool IsOperation { get => IsAddative || IsMultiplicative || IsParens; }
            public readonly bool IsAddative { get => isAdd || isSub; }
            public bool isAdd = false;
            public bool isSub = false;
            public readonly bool IsMultiplicative { get => isMult || isDiv; }
            public bool isMult = false;
            public bool isDiv = false;
            public bool IsValue { get => isImm || isVar; }
            public bool isImm = false;
            public bool isVar = false;
            //public bool IsParens { get => isLParens || isRParens; }
            public bool isLParens = false;
            public bool isRParens = false;
            public string primativeString;
            public Token(string primative) : this(primative, (_) => _, (_) => true) { }
            public Token(string primative, Func<string, string> normalizer, Func<string, bool> isValid)
            {
                isVar = Utility.isVariableRegex().IsMatch(primative);
                if (isImm = double.TryParse(primative, out double d)) primativeString = d.ToString();
                else if (isVar)
                {
                    if (isValid(primative)) primativeString = normalizer(primative.Trim());
                    else throw new FormulaFormatException("die exception");
                }
                else primativeString = primative;
                isDiv = primative.StartsWith('/');
                isMult = primative.StartsWith('*');
                isAdd = primative.StartsWith('+');
                isSub = primative.StartsWith('-');
                isLParens = primative.StartsWith('(');
                isRParens = primative.StartsWith(')');
                if (!(isAdd || isSub || IsMultiplicative || IsValue || isLParens || isRParens))
                    throw new FormulaFormatException("die exception");
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
                    return TokenType.val;
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
        private static IEnumerable<Token> GetNormalTokens(string formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            List<Token> ret = [];
            //slow plz fix
            foreach (var s in Utility.bounderies().Split(formula))
                if (!Utility.isWhiteSpaceRegex().IsMatch(s))
                    ret.Add(new(s, normalize, isValid));
            return ret;
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
        private static Dictionary<TokenType, TokenProcFunc> generateTokenProcessorDictionary(Stack<TokenNode> valueStack, Stack<Token> operatorStack, Func<string, bool> isValid)
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
                    valueStack.Push(new TokenNode(
                        operatorStack.Pop(),
                        left: valueStack.Pop(),
                        right: new TokenNode(token, null, null)
                        ));
                    return;
                }
                valueStack.Push(new TokenNode(token, null, null));
            }

            /// <summary>
            /// Proceed as above, using the looked-up value of t instead of t
            /// </summary>
            void varFunc(Token token)
            {
                if (!isValid(token.primativeString)) throw new ArgumentException("var bad");
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
                    if (valueStack.Count < 2) throw new FormulaFormatException("adding just one");
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
                if (operatorStack.Count != 0 && operatorStack.Peek().IsAddative)
                {
                    if (valueStack.Count < 2) throw new FormulaFormatException("unary add within parenthesis");
                    valueStack.Push(
                        new TokenNode(
                        primary: operatorStack.Pop(),
                        right: valueStack.Pop(),
                        left: valueStack.Pop()
                        ));
                }
                if (operatorStack.Count == 0 || !operatorStack.Pop().isLParens) throw new FormulaFormatException("Unmatched closing parenthesis");
                if (operatorStack.Count != 0 && operatorStack.Peek().IsMultiplicative)
                {
                    if (valueStack.Count < 2) throw new FormulaFormatException("idk something went wrong");
                    valueStack.Push(new TokenNode(
                        operatorStack.Pop(),
                        right: valueStack.Pop(),
                        left: valueStack.Pop()
                        ));
                }
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

    //tree index builder
    /*
    internal struct IB(uint s = 0) {
        public uint state = s;
        public static readonly IB Left = new((uint)D.Left);
        public static readonly IB Right = new((uint)D.Right);
        internal enum D { Left = 0, Right = 1 }
        public static IB operator +(IB one, IB two) => new((one.state << two.Length) | two.state);
        public static IB operator +(IB one, D two) => new((one.state << 1) | (uint)two);
        public static IB operator --(IB one) => new(one.state >> 1);
        public uint Length{ get => 32 - (uint)BitOperations.LeadingZeroCount(state); }
    }

    internal struct ContTree<T> where T : struct
    {
        List<T?> list;
        public T Get(uint index) => Get(new IB(index));
        public T Get(IB index) { throw new NotImplementedException(); }

        public T Set(uint index) => Set(new IB(index));
        public T Set(IB index) { throw new NotImplementedException(); }

    }*/
}
