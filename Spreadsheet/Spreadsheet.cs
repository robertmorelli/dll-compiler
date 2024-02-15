using SpreadsheetUtilities;
using System.Text.RegularExpressions;

namespace SS
{
    internal partial class Utility
    {
        [GeneratedRegex(@"^[_a-zA-Z][_a-zA-Z0-9]*$", options:
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking)]
        private static partial Regex _validName();
        private static readonly Regex validName = _validName();
        public static bool IsValidName(string s) => validName.IsMatch(s);
        [GeneratedRegex(@"^$", options:
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking)]
        private static partial Regex _nothing();
        private static readonly Regex nothing = _nothing();
        public static bool IsNothing(string s) => nothing.IsMatch(s);
    }

    public class Spreadsheet : AbstractSpreadsheet
    {
        private readonly DependencyGraph graph = new();
        protected override IEnumerable<string> GetDirectDependents(string name) => graph.GetDependees(name);
        HashSet<string> GetRecursiveDeps(string name)
        {
            Queue<string> dependees = new(graph.GetDependees(name));
            HashSet<string> recursiveDeps = [];
            while (dependees.TryDequeue(out string? d))
                if (recursiveDeps.Add(d))
                    foreach (var dd in graph.GetDependees(d))
                        dependees.Enqueue(dd);
            return recursiveDeps;
        }

        private readonly Dictionary<string, ICell> cells = [];
        public override IEnumerable<string> GetNamesOfAllNonemptyCells() => cells.Keys.AsEnumerable();
        private interface ICell
        {
            public object Value { get; }
            public object CurrentValue { get; }
            public void MarkDirty();
        }

        private readonly struct DoubleCell(double v) : ICell
        {
            public object Value { get => value; }
            public readonly object CurrentValue => value;
            public readonly void MarkDirty() { }

            private readonly double value = v;
        }

        private readonly struct StringCell(string s) : ICell
        {
            public object Value { get => value; }
            public readonly object CurrentValue => value;
            public readonly void MarkDirty() { }

            private readonly string value = s;
        }

        private struct FormulaCell(Formula f, Spreadsheet s) : ICell
        {
            private readonly Formula value = f;
            public object Value { get => Compute().GetType() == typeof(FormulaError) ? CachedValue : value; }
            public object CurrentValue => Compute();
            public void MarkDirty() { dirty = true; }

            private bool dirty = true;
            private readonly Spreadsheet spreadsheet = s;
            private object CachedValue = new FormulaError("Never Calculated Struct");

            private readonly double LookupUnsafe(string s) => (double)Lookup(s);
            private readonly object Lookup(string s)
            {
                object result = spreadsheet.cells[s].CurrentValue;
                if (result.GetType() == typeof(double)) return result;
                else if (result.GetType() == typeof(FormulaError))
                    return new FormulaError("Failed to look up [" + s + "] " + ((FormulaError)result).Reason);
                else return result;
            }

            object Compute()
            {
                if (CachedValue != null && dirty == false) return CachedValue;
                else return CachedValue = value.Evaluate(LookupUnsafe);
            }
        }

        public override object GetCellContents(string name)
        {
            if (!Utility.IsValidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.Value : "";
        }

        public override ISet<string> SetCellContents(string name, double number)
        {
            if (!Utility.IsValidName(name)) throw new InvalidNameException();
            cells[name] = new DoubleCell(number);
            return GetRecursiveDeps(name);
        }

        public override ISet<string> SetCellContents(string name, Formula formula)
        {
            if (!Utility.IsValidName(name)) throw new InvalidNameException();
            HashSet<string> recursiveDeps = [];
            {
                Queue<string> dependees = new(graph.GetDependees(name));
                while (dependees.TryDequeue(out string? d))
                    if (recursiveDeps.Add(d))
                        foreach (var dd in graph.GetDependees(d))
                            dependees.Enqueue(dd);
            }

            {
                var formVars = formula.GetVariables();
                if (recursiveDeps.Intersect(formVars).Any()) throw new CircularException();
                graph.ReplaceDependents(name, formVars);
            }

            cells[name] = new FormulaCell(formula, this);
            foreach (var toRecalculate in recursiveDeps)
                if (cells.TryGetValue(toRecalculate, out ICell? cell))
                    cell.MarkDirty();

            recursiveDeps.Add(name);
            return recursiveDeps;
        }

        public override ISet<string> SetCellContents(string name, string text)
        {
            if (!Utility.IsValidName(name)) throw new InvalidNameException();
            if (Utility.IsNothing(text)) return new HashSet<string>();
            cells[name] = new StringCell(text);
            return GetRecursiveDeps(name);
        }
    }
}