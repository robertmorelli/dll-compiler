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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS
{
    public class Spreadsheet : AbstractSpreadsheet
    {
        // private access dictionary of strings to Cell "cells" thats initialized to an empty dictionary
        // (good thing i left that comment so you could understand my code)
        private readonly Dictionary<string, Cell> cells = [];
        private readonly DependencyGraph graph = new();

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override object GetCellContents(string name) => cells[name].formula;

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
        public override ISet<string> SetCellContents(string name, string text) => SetCellContents(name, new Formula(text));

        /// <inheritdoc/>
        /// <summary>
        /// This one actually does stuff
        /// </summary>
        /// <param name="name"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public override ISet<string> SetCellContents(string name, Formula formula)
        {
            Cell cell = new(name, formula);
            if (IsRecursive(cell)) throw new ArgumentException();
            // set the cell to be a new Cell of a Formula
            // and then set the dependees to be the variables from said new formula
            graph.ReplaceDependents(name, (cells[name] = cell).formula.GetVariables());
            // the line below literally kills osama bin laden. no lie
            return RecursiveDeps(name);
        }

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name) => graph.GetDependents(name);

        /// <summary>
        /// All dependencies at all deps
        /// </summary>
        /// <param name="name">
        /// The var name to get deps
        /// </param>
        /// <returns>
        /// inda description homie
        /// </returns>
        private HashSet<string> RecursiveDeps(string name) => AllDeps(GetDirectDependents(name));

        /// <summary>
        /// All dependencies at all depths for a formula
        /// </summary>
        /// <param name="topLevelDependencies">
        /// the dependencies to check dependencies for
        /// </param>
        /// <returns>
        /// inda description homie
        /// </returns>
        private HashSet<string> AllDeps(IEnumerable<string> topLevelDependencies)
        {
            //we dont support using the callstack as a data queue in our household
            Queue<string> dependees = new(topLevelDependencies);
            HashSet<string> recursiveDeps = [];
            //level order traversal. if you are confused maybe read a book?
            while (dependees.TryDequeue(out string? d)) if(recursiveDeps.Add(d)) foreach (var dd in GetDirectDependents(d)) dependees.Enqueue(dd);
            return recursiveDeps;
        }

        /// <summary>
        /// Determine if a cell is a valid addition (non-recursive)
        /// </summary>
        /// <param name="cell">
        /// Cell we are deciding if we can add
        /// </param>
        /// <returns>
        /// inda description homie
        /// </returns>
        private bool IsRecursive(Cell cell) => AllDeps(cell.formula.GetVariables()).Contains(cell.ID);



        /// <summary>
        /// For storing a specific cell with an id and formula
        /// IDEK know why I need this. the instructions say I need it
        /// </summary>
        private struct Cell(string name, Formula f)
        {
            public string ID = name;
            public Formula formula = f;
        }
    }
}
