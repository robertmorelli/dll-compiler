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

using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Formula
{

    public static partial class Utility
    {

        //speed up regex performance
        //no backtracking makes this a DFA instead of NFA
        //GeneratedRegex makes these all compiled at compile time
        //(as opposed to run time)
        private const RegexOptions RegexOptions =
            System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace |
            System.Text.RegularExpressions.RegexOptions.NonBacktracking;

        [GeneratedRegex(@"^\s*$", options: RegexOptions)] public static partial Regex IsWhiteSpaceRegex();
        [GeneratedRegex(@"^\p{L}[\p{L}\p{Nd}]*$", options: RegexOptions)] public static partial Regex IsVariableRegex();
        [GeneratedRegex(@"\b((?:\p{Nd}+E[-+]?|\p{Nd}*?\.?)?\d+?)\b|([-)*(+/])|\b(\p{L}[\p{L}\p{Nd}]*?)\b", options: RegexOptions)] public static partial Regex Boundaries();
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

        private readonly Token[] _tokens;
        private readonly string[] _varTokens;
        private TokenNode _executableAst;
        private readonly bool _throwDbzEveryTime = false;
        private int _hashCodeCash = 0;
        private bool _hasHashCode = false;

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
            var tokenProcessor = GenerateTokenProcessorDictionary(valueStack, operatorStack, isValid);
            _tokens = Utility
                .Boundaries()
                .Matches(formula)
                .Select((m)=> new Token(m.Value))
                .ToArray();
            if (_tokens.Length == 0) throw new FormulaFormatException("nothing");

            tokenProcessor[TokenType.OpenParen](new Token("("));
            foreach (var token in _tokens) tokenProcessor[token.Type](token);
            tokenProcessor[TokenType.ClosedParen](new Token("("));
            tokenProcessor[TokenType.Multiplicative](new Token("*"));
            tokenProcessor[TokenType.Val](new Token("1"));

            _executableAst = valueStack.Pop();
            if ((valueStack.Count > 0) || (operatorStack.Count > 0)) throw new FormulaFormatException("unmatched parenthesis or other operator");
            _throwDbzEveryTime = _executableAst.IsDbzConst();
            _executableAst = _executableAst.Optimized();
            _varTokens = _tokens
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
            if (_throwDbzEveryTime) return new FormulaError("divide by zero for string: " + ToString());
            try { return _executableAst.Value(lookup); }
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
        public IEnumerable<string> GetVariables() => _varTokens;

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
            _tokens
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
            if (_hasHashCode) return _hashCodeCash;
            _hashCodeCash = ToString().GetHashCode();
            _hasHashCode = true;
            return _hashCodeCash;
        }

        //types of tokens for the parse algorithm
        public enum TokenType
        {
            Multiplicative = 0,
            Additive = 1,
            Var = 2,
            Val = 3,
            OpenParen = 4,
            ClosedParen = 5,
        }

        /// <summary>
        /// element to build AST out of
        /// elements with primary ops are to be evaluated
        /// elements with primary vals are either immediate value or var
        /// </summary>
        public struct TokenNode
        {
            //non-optimizing constructor
            public TokenNode(Token primary, TokenNode? left, TokenNode? right)
            {
                this._primary = primary;
                _leftRef = new(left);
                _rightRef = new(right);
                _constValue = primary.isImm ? double.Parse(primary.primativeString) : double.NaN;
                if (LeftChild != null)
                    if (RightChild != null)
                    {
                        var realLeft = (TokenNode)LeftChild;
                        var realRight = (TokenNode)RightChild;
                        //NaN o K = NaN unless NaN * 0 in some cases does accidental optimizations to remove vars
                        _constValue = primary.primativeString switch
                        {
                            "*" => realLeft._constValue * realRight._constValue,
                            "/" => realLeft._constValue / realRight._constValue,
                            "+" => realLeft._constValue + realRight._constValue,
                            "-" => realLeft._constValue - realRight._constValue,
                            _ => double.NaN
                        };
                    }
            }

            //optimizes AST tree and returns best version this code can produce
            public readonly TokenNode Optimized(bool unsafeOptimizations = true)
            {
                if (_primary.IsValue) return this;
                if (HasConstValue) return new TokenNode(new Token(_constValue.ToString("F")), null, null);

                if (_primary.isVar) return this;
                var realLeft = ((TokenNode)LeftChild).Optimized(unsafeOptimizations);
                var realRight = ((TokenNode)RightChild).Optimized(unsafeOptimizations);
                if (_primary.isAdd)
                {
                    // 0 + % = %
                    if (realLeft._constValue.Equals(0)) return realRight;
                    // % + 0 = %
                    if (realRight._constValue.Equals(0)) return realLeft;
                }
                if (_primary.isMult)
                {
                    // 1 * % = %
                    if (realLeft._constValue.Equals(1)) return realRight;
                    // % * 1 = % 
                    if (realRight._constValue.Equals(1)) return realLeft;
                }
                if (_primary.isSub)
                {
                    // % - 0 = %
                    if (realRight._constValue.Equals(0)) return realLeft;
                }
                if (_primary.isDiv)
                {
                    // % / 1 = %
                    if (realRight._constValue.Equals(1)) return realLeft;
                }
                if (!unsafeOptimizations) return this;
                //reciprocal const division % / k1, k2 = 1/k1, do % * k2
                if (_primary.isDiv && realRight.HasConstValue)
                {
                    return new TokenNode(
                        new Token("*"),
                        realLeft,
                        new TokenNode(
                            new Token(
                                (1 / realRight._constValue).ToString("F")),
                                null,
                                null
                        ));
                }
                // TODO: tree balancing ((%1 * %2) * %3) * %4 = (%1 * %2) * (%3 * %4)
                // btw technically not valid for float types

                // TODO: %1 + %1 = 2 * %1
                // TODO: (k1 * %1) + %1, k2 = k1 + 1, do k2 * %
                

                return new TokenNode(this._primary, realLeft, realRight);
            }

            public readonly bool IsDbzConst()
            {
                var rightMaybe = RightChild;
                var leftMaybe = LeftChild;
                if (rightMaybe == null || leftMaybe == null) return false;
                var right = (TokenNode)rightMaybe;
                var left = (TokenNode)leftMaybe;
                if (right.IsDbzConst() || left.IsDbzConst()) return true;
                return _primary.isDiv && right is { HasConstValue: true, _constValue: 0 };
            }

            //does a preorder traversal lisp-y string for debugging "k" indicates calculated constant vals
            public readonly string ToString2(Func<string, double>? lo = null, int depth = 0)
            {
                lo ??= (_) => 1;
                return
                    "\n" + new string(' ', depth * 4) +
                    (LeftChild != null ? "(" : "") +
                    _primary.primativeString.ToString() +
                    (HasConstValue ? " K" + (LeftChild != null ? " :" + _constValue : "") : "") +
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
                    _primary.primativeString.ToString() +
                    RightChild?.ToString(lo) +
                    (LeftChild != null ? ")" : "");// +
                                                   //(LeftChild != null ? (HasConstValue ? "=" + constValue : "") : "");
            }

            public void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields)
            {
                RightChild?.Compile(gen, fields);
                LeftChild?.Compile(gen, fields);
                _primary.Compile(gen, fields);
            }

            //who said there were no pointers in "safe" c#?
            private class ReferenceLmao(TokenNode? tn) { public TokenNode? tn = tn; }
            private readonly ReferenceLmao _leftRef;
            private readonly ReferenceLmao _rightRef;
            //ease of use get from TokenNode*
            private readonly TokenNode? LeftChild { get => _leftRef.tn; }
            private readonly TokenNode? RightChild { get => _rightRef.tn; }

            //primary token for this node
            private readonly Token _primary;

            //NaN indicates var dependence. sue me. cry about it. shout at the sky even
            private readonly bool HasConstValue => !double.IsNaN(_constValue);

            //for optimizing
            private readonly double _constValue;

            //should never really produce NaNs. I thing 10/(1/1E-25) or something might trigger this
            //you would have to divide by infinite
            public readonly double Value(Func<string, double> lookup)
            {
                if (HasConstValue) return _constValue;
                if (_primary.isVar) return lookup(_primary.primativeString);
                if (LeftChild == null || RightChild == null) return double.NaN;
                var leftValue = ((TokenNode)LeftChild).Value(lookup);
                var rightValue = ((TokenNode)RightChild).Value(lookup);
                if (rightValue.Equals(0) && _primary.isDiv) throw new ArgumentException("divide by zero");
                return _primary.primativeString switch
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
        public readonly struct Token
        {
            /// <summary>
            /// these vars exist because im too lazy to make
            /// an enum for this. anyway this is pretty self explanitory
            /// this is all token types with some inheritance
            /// </summary>
            //public bool IsOperation { get => IsAddative || IsMultiplicative || IsParens; }
            public readonly bool IsAddative { get => isAdd || isSub; }
            public readonly bool isAdd = false;
            public readonly bool isSub = false;
            public readonly bool IsMultiplicative { get => isMult || isDiv; }
            public readonly bool isMult = false;
            public readonly bool isDiv = false;
            public bool IsValue => isImm || isVar;
            public readonly bool isImm = false;
            public readonly bool isVar = false;
            //public bool IsParens { get => isLParens || isRParens; }
            public readonly bool isLParens = false;
            public readonly bool isRParens = false;
            public readonly string primativeString;
            public Token(string primative) : this(primative, (_) => _, (_) => true) { }
            public Token(string primative, Func<string, string> normalizer, Func<string, bool> isValid)
            {
                isVar = Utility.IsVariableRegex().IsMatch(primative);
                if (isImm = double.TryParse(primative, out double d)) primativeString = d.ToString("F");
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
                    if (IsAddative) return TokenType.Additive;
                    if (IsMultiplicative) return TokenType.Multiplicative;
                    if (isVar) return TokenType.Var;
                    if (isLParens) return TokenType.OpenParen;
                    if (isRParens) return TokenType.ClosedParen;
                    return TokenType.Val;
                }
            }
            public readonly void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields)
            {
                if (isAdd) gen.Emit(OpCodes.Add);
                else if (isSub) gen.Emit(OpCodes.Sub);
                else if (isDiv) gen.Emit(OpCodes.Div);
                else if (isMult) gen.Emit(OpCodes.Mul);
                else if (isImm)
                {
                    gen.Emit(OpCodes.Ldc_R8, double.Parse(primativeString));
                }
                else if (isVar)
                {
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldfld, fields[primativeString]);
                }
            }
        }

        private delegate void TokenProcFunc(Token token);
        

        public delegate int Lookup(string variableName);

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
        /// <param name="isValid">
        /// The variable lookup used by the variable delegate
        /// </param>
        /// <returns>
        /// A dictionary with keys and values for appropriate operations to perform per regex
        /// </returns>
        /// <exception cref="Exception">
        /// This function will not throw errors, the returned delegates will throw errors
        /// for improper formulas
        /// </exception>
        private static Dictionary<TokenType, TokenProcFunc> GenerateTokenProcessorDictionary(Stack<TokenNode> valueStack, Stack<Token> operatorStack, Func<string, bool> isValid)
        {
            // dictionary of regex matched with correct response.
            return new Dictionary<TokenType, TokenProcFunc> {
                {TokenType.Val, ImmFunc },
                {TokenType.Var, VarFunc },
                {TokenType.Additive, AddativeFunc },
                {TokenType.Multiplicative, Multiplicativefunc },
                {TokenType.OpenParen, OpenParenFunc },
                {TokenType.ClosedParen, CloseParenFunc },
            };

            /// <summary>
            /// Proceed as above, using the looked-up value of t instead of t
            /// </summary>
            void VarFunc(Token token)
            {
                if (!isValid(token.primativeString)) throw new ArgumentException("var bad");
                ImmFunc(token);
            }

            /// <summary>
            /// Push t onto the operator stack
            /// 
            /// duplicate of mult. cry about it
            /// </summary>
            void OpenParenFunc(Token token) => operatorStack.Push(token);

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
            void CloseParenFunc(Token token)
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
                if (operatorStack.Count == 0 || !operatorStack.Peek().IsMultiplicative) return;
                if (valueStack.Count < 2) throw new FormulaFormatException("idk something went wrong");
                valueStack.Push(new TokenNode(
                    operatorStack.Pop(),
                    right: valueStack.Pop(),
                    left: valueStack.Pop()
                ));
            }

            /// <summary>
            /// Push t onto the operator stack
            /// 
            /// also unnecessary func wrapper. try crying if you dont like it
            /// </summary>
            void Multiplicativefunc(Token token) => operatorStack.Push(token);

            /// <summary>
            /// "If + or - is at the top of the operator stack,
            /// pop the value stack twice and the operator stack once,
            /// then apply the popped operator to the popped numbers,
            /// then push the result onto the value stack.
            /// 
            /// Push t onto the operator stack"
            /// </summary>
            void AddativeFunc(Token token)
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
            /// If * or / is at the top of the operator stack, pop the value stack,
            /// pop the operator stack, and apply the popped operator to the popped
            /// number and t. Push the result onto the value stack.
            /// 
            /// Otherwise, push t onto the value stack.
            /// </summary>
            void ImmFunc(Token token)
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
        }

        public void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields) => _executableAst.Compile(gen, fields);
        
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
