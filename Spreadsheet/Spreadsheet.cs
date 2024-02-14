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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            /*try
            {
                graph.AddDependency("a1", "a1");
                base.GetCellsToRecalculate("a1");
            }
            catch (Exception)
            {
                graph.RemoveDependency("a1", "a1");
            }*/
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
        public override ISet<string> SetCellContents(string name, double number)
        {
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            cells[name] = new DoubleCell(number);
            return GetRecursiveDeps(name).ToHashSet();
        }

        /// <inheritdoc/>
        /// <summary>
        /// This one calls the formula one
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, Formula formula)
        {
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            var formVars = formula.GetVariables();

            //throw circulars
            if (formVars.Contains(name)) throw new CircularException();


            Queue<string> dependents = new(graph.GetDependees(name));
            HashSet<string> recursiveDeps = [];

            while (dependents.TryDequeue(out string? d))
            {
                if (d.Equals(name))
                {
                    throw new CircularException();
                }
                if (recursiveDeps.Add(d))
                {
                    foreach (var dd in graph.GetDependees(d))
                    {
                        dependents.Enqueue(dd);
                    }
                }
            }



            graph.ReplaceDependents(name, formVars);
            cells[name] = new FormulaCell(name, formula, this);
            foreach (var toRecalculate in recursiveDeps)
                if(cells.TryGetValue(toRecalculate, out ICell? cell))
                    cell.MarkDirty();
            recursiveDeps.Add(name);
            return recursiveDeps;
        }

        /// <inheritdoc/>
        /// <summary>
        /// This one actually does stuff
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, string text)
        {
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            if ((text = text.Trim()).Equals("")) return new HashSet<string>();
            cells[name] = new StringCell(text);
            return GetRecursiveDeps(name).ToHashSet();
        }

        /// <summary>
        /// just like the one it overrides
        /// in testing 60-100% faster than refernce code and get substantially worse with
        /// larger chains (chains in excess of 5k result in over 200% performance increase)
        /// cannot be made lazy due to full traversal required before
        /// order is determined
        /// 
        /// order does not need to be set as such when using lazy cell recalculation
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        new Stack<string> GetCellsToRecalculate(string name)
        {
            var changed = new Stack<string>();
            var visited = new HashSet<string>() { name };
            void ProcDep(string dep)
            {
                foreach (string n in GetDirectDependents(dep))
                    if (n.Equals(name)) throw new CircularException();
                    else if (visited.Add(n)) ProcDep(n);
                changed.Push(dep);
            }
            ProcDep(name);
            return changed;
        }

        Stack<string> GetRecursiveDeps(string name)
        {
            // we DO NOT SUPPORT using the call stack as a data queue in this household
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
            return depStack;
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
            public object CurrentValue { get; }
            public void MarkDirty();
        }

        private readonly struct DoubleCell(double v) : ICell
        {
            private readonly double value = v;
            public readonly object CurrentValue => value;
            public readonly void MarkDirty() { }
            public readonly void Recalculate() { }
        }

        private readonly struct StringCell(string s) : ICell
        {
            private readonly string value = s;
            public readonly object CurrentValue => value;
            public readonly void MarkDirty() { }
            public readonly void Recalculate() { }
        }


        private struct FormulaCell : ICell
        {
            private bool dirty = true;
            void ICell.MarkDirty() { dirty = true; }
            //obvious why formula is here
            private readonly Formula formula;
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
            public FormulaCell(string n, Formula f, Spreadsheet s)
            {
                spreadsheet = s;
                name = n;
                formula = f;
                try
                {
                    FirstOrderDeps = formula.GetVariables().ToHashSet();
                }
                catch (FormulaFormatException e)
                {
                    CachedValue = new FormulaError(e.Message);
                    FirstOrderDeps = [];
                }
            }

            // hides the implementation of cached values
            object ICell.CurrentValue { get => Compute(); }

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
            object Compute()
            {
                //no need to do other work.
                //if this isnt a valid formula
                //this cannot depend on any formula and therefore
                //cannot be called
                if (CachedValue != null && dirty == false) return (double)CachedValue;
                CachedValue = formula.Evaluate(LookupUnsafe);
                dirty = false;
                if (CachedValue != null) return (double)CachedValue;
                return new FormulaError();
            }
        }
    }
}
