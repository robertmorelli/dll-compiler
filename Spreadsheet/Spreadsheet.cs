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
/// My implementation of AbstractSpreadsheet
/// </summary>


using SpreadsheetUtilities;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SS
{
    internal partial class Utility
    {
        [GeneratedRegex(@"^[_a-zA-Z][_a-zA-Z0-9]*$", options:
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking)]
        public static partial Regex validName();
    }
    public class Spreadsheet : AbstractSpreadsheet
    {
        // private access dictionary of strings to Cell "cells" thats initialized to an empty dictionary
        // (good thing i left that comment so you could understand my code)
        private readonly Dictionary<string, ICell> cells = [];
        private readonly DependencyGraph graph = new();

        public Spreadsheet()
        {
            try
            {
                graph.AddDependency("a1", "a1");
                base.GetCellsToRecalculate("a1");
            }
            catch (Exception)
            {
                graph.RemoveDependency("a1", "a1");
            }
        }

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object GetCellContents(string name)
        {
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.CurrentValue : "";
        }

        /// <inheritdoc/>
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells() => cells.Keys.AsEnumerable();

        /// <inheritdoc/>
        /// <summary>
        /// This one cals the string one
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, double number) => SetCellContents(name, number.ToString());

        /// <inheritdoc/>
        /// <summary>
        /// This one calls the formula one
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, string text)
        {
            if ((text = text.Trim()).Equals("")) return new HashSet<string>();
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            ICell cell = new FormulaCell(name, text, this);
            //we dont support using the callstack as a data queue in our household
            Queue<string> dependents = new(cell.Dependencies);
            HashSet<string> recursiveDeps = [];
            //level order traversal. if you are confused maybe read a book?
            while (dependents.TryDequeue(out string? d))//question mark gets promoted away in this line so dont worry
                if (recursiveDeps.Add(d))//if its already in there skip the children
                    foreach (var dd in graph.GetDependents(d))
                        if (dd.Equals(name)) throw new CircularException();//throws circular before add
                        else dependents.Enqueue(dd);//so that the descendents will be processed
            // set the cell to be a new Cell of a Formula
            // and then set the dependees to be the variables from said new formula
            graph.ReplaceDependents(name, (cells[name] = cell).Dependencies);

            //recalculate Necessary
            var cellsToRecalculate = GetCellsToRecalculate(name);
            foreach (var toRecalculate in cellsToRecalculate) cells[toRecalculate].Recalculate();
            return cellsToRecalculate.ToHashSet();
        }

        /// <inheritdoc/>
        /// <summary>
        /// This one actually does stuff
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, Formula formula) => SetCellContents(name, formula.ToString());

        /// <summary>
        /// just like the one it overrides
        /// in testing 60-100% faster than refernce code and get substantially worse with
        /// larger chains (chains in excess of 5k result in over 200% performance increase)
        /// cannot be made lazy due to full traversal required before
        /// order is determined
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        new IEnumerable<string> GetCellsToRecalculate(string name)
        {
            //remove later
            base.GetCellsToRecalculate(name);

            //we DO NOT SUPPORT using the call stack as a data queue in this household
            Stack<string> depStack = new();
            Queue<string> depQueue = new();
            //in queue d is replaced by its dependencies. most dependent at top of stack
            //then we remove duplicates (from lower on stack)
            //then we reverse the stack so the lest dependent comes first
            string? dep = name;
            do
            {
                foreach (var depOfDep in GetDirectDependents(dep)) depQueue.Enqueue(depOfDep);
                depStack.Push(dep);
            } while (depQueue.TryDequeue(out dep));
            return depStack.Distinct().Reverse();
        }

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name) => graph.GetDependees(name);

        /// <summary>
        /// For storing a specific cell with an id and formula
        /// IDEK know why I need this. the instructions say I need it
        /// </summary>
        private interface ICell
        {
            public ISet<string> Dependencies { get; }
            public void Recalculate();
            public object CurrentValue { get; }
        }


        private struct FormulaCell : ICell
        {
            //obvious why formula is here
            private readonly Formula? formula;
            //needs spreedsheet reference to fetch cells
            private readonly Spreadsheet spreadsheet;

            //for implementation hiding for the above interface
            private readonly string name;
            private readonly HashSet<string> FirstOrderDeps;
            private object CachedValue = new FormulaError("Never Calculated");

            /// <summary>
            /// formula version of a cell
            /// this is so i can add more stuff later
            /// </summary>
            /// <param name="n">for the id</param>
            /// <param name="f">for the formula</param>
            /// <param name="s">need a spreadsheet reference for var lookup</param>
            /// <exception cref="CircularException">
            /// if a top level reference refers directly to the name
            /// </exception>
            public FormulaCell(string n, string f, Spreadsheet s)
            {
                spreadsheet = s;
                name = n;
                try
                {
                    formula = new Formula(f);
                    FirstOrderDeps = formula.GetVariables().ToHashSet();
                    if (FirstOrderDeps.Contains(n)) throw new CircularException();
                    Recalculate();
                }
                catch (FormulaFormatException e)
                {
                    CachedValue = new FormulaError(e.Message);
                    FirstOrderDeps = [];
                }
            }

            // hides the implementation of dependency fetching
            // this may allow for more cell types in the future
            ISet<string> ICell.Dependencies { get => FirstOrderDeps; }

            // hides the implementation of cached values
            readonly object ICell.CurrentValue { get => CachedValue; }

            /// <summary>
            /// inherited methods cannot be called from the constructor so
            /// this is the separation of internal and external recalculate
            /// </summary>
            void ICell.Recalculate() => Recalculate();

            /// <summary>
            /// lookup var value for its cached value
            /// </summary>
            /// <param name="s">the var name to lookup</param>
            /// <returns></returns>
            private readonly object Lookup(string s) => spreadsheet.cells[s].CurrentValue;

            /// <summary>
            /// lookup and assume safety
            /// could be improved to attempt force chain refresh
            /// </summary>
            /// <param name="s"></param>
            /// <returns></returns>
            private readonly double LookupUnsafe(string s) => (double)Lookup(s);

            /// <summary>
            /// use the lookup functions above to recalculate the current value of the cell
            /// this should be called only when its dependencies change
            /// or
            /// when its initialized in case it is a const expression
            /// </summary>
            void Recalculate()
            {
                CachedValue = formula?.Evaluate(LookupUnsafe) ?? CachedValue;
                //no need to do other work.
                //if this isnt a valid formula
                //this cannot depend on any formula and therefore
                //cannot be called
            }
        }
    }
}
