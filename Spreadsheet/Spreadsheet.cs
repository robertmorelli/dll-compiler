using SpreadsheetUtilities;
using System.Net.Http.Headers;
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
        public static bool IsInvalidName(string s) => !validName.IsMatch(s);


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
            HashSet<string> recursiveDeps = [];
            {
                Queue<string> dependees = new();
                string? dep = name;
                do
                {
                    if (recursiveDeps.Add(dep))
                        foreach (string depOfDep in graph.GetDependees(dep))
                            dependees.Enqueue(depOfDep);
                } while (dependees.TryDequeue(out dep));
            }
            return recursiveDeps;
        }

        private readonly Dictionary<string, ICell> cells = [];
        public override IEnumerable<string> GetNamesOfAllNonemptyCells() => cells.Keys.AsEnumerable();
        private interface ICell
        {
            public object Value { get; }
            public object Contents { get; }
            public void Compute();
        }

        private readonly struct DoubleCell(double d) : ICell
        {
            public readonly object Value => value;
            public readonly object Contents => value;
            public void Compute() { }

            private readonly double value = d;
        }

        private readonly struct StringCell(string s) : ICell
        {
            public readonly object Value => value;
            public readonly object Contents => value;
            public void Compute() { }

            private readonly string value = s;
        }

        private struct FormulaCell(Formula f, Spreadsheet s) : ICell
        {
            private readonly Formula _content = f;
            private readonly Spreadsheet spreadsheet = s;
            public readonly object Contents => _content;
            public readonly object Value => _currentValue;
            private object _currentValue = new FormulaError("Never Calculated Struct");
            public void Compute()
            {
                Dictionary<string, double> lookup = [];
                double Lookup(string s) => lookup[s];
                foreach (string dep in _content.GetVariables())
                {
                    if (spreadsheet.cells.TryGetValue(dep, out ICell? cell))
                    {
                        var val = cell.Value;
                        if (val.GetType() == typeof(double))
                        {
                            lookup[dep] = (double)val;
                        }
                        else
                        {
                            _currentValue = new FormulaError("Dependency Not Valid");
                            return;
                        }
                    }
                    else
                    {
                        _currentValue = new FormulaError("Dependency Not Available");
                        return;
                    }
                }
                _currentValue = _content.Evaluate(Lookup);
            }
        }

        public override object GetCellContents(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.Contents : "";
        }

        public object GetCellValue(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? cell) ? cell.Value : "";
        }

        private void RecompInOrder(string name)
        {
            HashSet<string> visited = [];
            Stack<string> changed = new();
            Stack<string> toDo = new();
            string? n = name;
            do
                if (visited.Add(n))
                {
                    changed.Push(n);
                    foreach (string dep in graph.GetDependees(n))
                        if (dep.Equals(name)) throw new CircularException();
                        else changed.Push(dep);
                }
                else toDo.Push(n);
            while (changed.TryPop(out n));
            while (toDo.TryPop(out n))
                if (cells.TryGetValue(n, out ICell? cell))
                    cell.Compute();
        }














        public override ISet<string> SetCellContents(string name, string text)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            graph.ReplaceDependents(name, []);
            if (Utility.IsNothing(text)) return new HashSet<string>();
            cells[name] = new StringCell(text);
            RecompInOrder(name);
            return GetRecursiveDeps(name);
        }

        public override ISet<string> SetCellContents(string name, double number)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            graph.ReplaceDependents(name, []);
            cells[name] = new DoubleCell(number);
            RecompInOrder(name);
            return GetRecursiveDeps(name);
        }

        public override ISet<string> SetCellContents(string name, Formula formula)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            HashSet<string> recursiveDeps = [];
            {
                Queue<string> dependees = new(graph.GetDependees(name));
                while (dependees.TryDequeue(out string? dep))
                    if (recursiveDeps.Add(dep))
                        foreach (var depOfDep in graph.GetDependees(dep))
                            dependees.Enqueue(depOfDep);
            }

            {
                var formVars = formula.GetVariables();
                if (formVars.Contains(name) || recursiveDeps.Intersect(formVars).Any())
                    throw new CircularException();
                graph.ReplaceDependents(name, formVars);
            }

            cells[name] = new FormulaCell(formula, this);
            RecompInOrder(name);
            recursiveDeps.Add(name);
            return recursiveDeps;
        }
    }
}