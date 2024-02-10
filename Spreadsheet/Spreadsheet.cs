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
/// I am not using the POS tier aweful GetCellsToRecalculate and Visit methods
/// These are aweful implementations. see the readme for why I hate them.
/// </summary>


using SpreadsheetUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace SS
{
    internal partial class Utility
    {
        [GeneratedRegex(@"^[_a-zA-Z][_a-zA-Z0-9]+$", options:
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

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object GetCellContents(string name)
        {
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.Value() : "";
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
        public override ISet<string> SetCellContents(string name, double number) => SetCellContents(name, new Formula(number.ToString()));

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
            return SetCellContents(name, new Formula(text));
        }

        /// <inheritdoc/>
        /// <summary>
        /// This one actually does stuff
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, Formula formula)
        {
            if (!Utility.validName().IsMatch(name)) throw new InvalidNameException();
            ICell cell = new FormulaCell(name, formula, this);

            //we dont support using the callstack as a data queue in our household
            Queue<string> dependees = new(cell.Dependencies);
            HashSet<string> recursiveDeps = [];
            //level order traversal. if you are confused maybe read a book?
            while (dependees.TryDequeue(out string? d))//question mark gets promoted away in this line so dont worry
                if (recursiveDeps.Add(d))//if its already in there skip the children
                    if (recursiveDeps.Contains(cell.ID)) throw new CircularException();//throws circular before add
                    else foreach (var dd in GetDirectDependents(d))
                            dependees.Enqueue(dd);//so that the descendents will be processed

            // set the cell to be a new Cell of a Formula
            // and then set the dependees to be the variables from said new formula
            graph.ReplaceDependents(name, (cells[name] = cell).Dependencies);
            // the line below literally kills osama bin laden. no lie
            return recursiveDeps;
        }

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name) => graph.GetDependents(name);

        /// <summary>
        /// For storing a specific cell with an id and formula
        /// IDEK know why I need this. the instructions say I need it
        /// </summary>
        private interface ICell
        {
            public string ID { get; }
            public ISet<string> Dependencies { get; }
            public object Value();
        }

        /// <summary>
        /// formula version of a cell
        /// this is so i can add more stuff later
        /// </summary>
        /// <param name="n">for the id</param>
        /// <param name="f">for the formula</param>
        /// <param name="s">need a spreadsheet reference for var lookup</param>
        private readonly struct FormulaCell(string n, Formula f, Spreadsheet s) : ICell
        {
            private readonly Formula formula = f;
            private readonly Spreadsheet spreadsheet = s;
            private readonly string name = n;

            string ICell.ID { get => name; }
            ISet<string> ICell.Dependencies { get => formula.GetVariables().ToHashSet(); }
            object ICell.Value()
            {
                Dictionary<string, double> validVars = [];
                foreach (string dep in formula.GetVariables())
                {
                    var val = spreadsheet.GetCellContents(dep);
                    if (val.GetType() == typeof(double)) validVars[dep] = (double)val;
                    else return new FormulaError();
                }
                return formula.Evaluate((s) => (double)validVars[s]);
            }
        }
    }
}
