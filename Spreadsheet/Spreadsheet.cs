using SpreadsheetUtilities;
using System.Text.RegularExpressions;
using System.Xml;

namespace SS
{
    internal partial class Utility
    {
        [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9]*$", options:
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
        protected readonly DependencyGraph graph = new();
        protected override IEnumerable<string> GetDirectDependents(string name) => graph.GetDependees(name);
        protected readonly Dictionary<string, ICell> cells = [];
        protected interface ICell
        {
            public object Value { get; }
            public object Contents { get; }
            public void Compute();
        }

        protected readonly struct DoubleCell(double d) : ICell
        {
            public readonly object Value => value;
            public readonly object Contents => value;
            public void Compute() { }

            private readonly double value = d;
        }

        protected readonly struct StringCell(string s) : ICell
        {
            public readonly object Value => value;
            public readonly object Contents => value;
            public void Compute() { }

            private readonly string value = s;
        }

        protected struct FormulaCell(Formula f, Spreadsheet s) : ICell
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

        //emulate recursive stack frame
        //firsthalf is a proxy for the return pointer
        //(which is either at the start of the function of halfway through)
        //name is a string stack variable
        protected struct IRecompStackFrame { public string name; public bool firstHalf; };

        /// <summary>
        /// virtual stack implementation of the base.GetCellsToRecalculate
        /// </summary>
        /// <inheritdoc/>
        /// <param name="start">start cell</param>
        /// <returns></returns>
        /// <exception cref="CircularException">if the dependencies are circular</exception>
        new protected Stack<string> GetCellsToRecalculate(string start)
        {
            try
            {
                base.GetCellsToRecalculate(name);
            }
            catch { }
            // c# does not support tail call optimizations
            Stack<IRecompStackFrame> virtualCallStack = new();

            //same as original
            HashSet<string> visited = [];
            //obligatory "linked list bad" comment (because linked lists are BAD!)
            Stack<string> changed = new();

            //"Call" Visit(start...)
            IRecompStackFrame frame = new() { name = start, firstHalf = true };
            do
            {
                if (frame.firstHalf)//ret pops either &Visit or &Visit + K
                {
                    visited.Add(frame.name);
                    frame.firstHalf = false;
                    virtualCallStack.Push(frame);
                    //to exactly copy behavior this must itterate in reverse
                    foreach (string n in GetDirectDependents(frame.name))
                    {
                        if (n.Equals(start))
                        {
                            throw new CircularException();
                        }
                        else if (!visited.Contains(n))
                        {
                            //"Call" Visit(n...)
                            virtualCallStack.Push(new() { name = n, firstHalf = true });
                        }
                    }
                }
                else
                {
                    changed.Push(frame.name);
                }
            } while (virtualCallStack.TryPop(out frame));
            return changed;
        }


        protected override IList<string> SetCellContents(string name, string text)
        {
            if (Utility.IsNothing(text)) return [];
            graph.ReplaceDependents(name, []);
            var toDo = GetCellsToRecalculate(name);
            cells[name] = new StringCell(text);
            return [.. toDo];
        }

        protected override IList<string> SetCellContents(string name, double number)
        {
            graph.ReplaceDependents(name, []);
            var toDo = GetCellsToRecalculate(name);
            cells[name] = new DoubleCell(number);
            return [.. toDo];
        }

        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            if (formula.GetVariables().Contains(name)) throw new CircularException();
            var toDo = GetCellsToRecalculate(name);//can throw circular
            cells[name] = new FormulaCell(formula, this);
            graph.ReplaceDependents(name, toDo);
            return [.. toDo];
        }

        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
        }

        public override bool Changed {
            get => throw new NotImplementedException();
            protected set => throw new NotImplementedException();
        }

        public override IEnumerable<string> GetNamesOfAllNonemptyCells() => cells.Keys.AsEnumerable();


        public override string GetSavedVersion(string filename)
        {
            throw new NotImplementedException();
        }

        public override void Save(string filename)
        {
            throw new NotImplementedException();
        }

        public override string GetXML()
        {
            StringWriter stringWriter = new();
            using XmlWriter xmlWriter = XmlWriter.Create(stringWriter);
            DoXMLWriting(xmlWriter);
            return stringWriter.ToString();
        }

        protected void DoXMLWriting(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartDocument();//<xml ...
            xmlWriter.WriteStartElement("spreadsheet");//<spreadsheet>
            foreach (var (name, cell) in cells)
            {
                xmlWriter.WriteStartElement("cell");//<cell>
                xmlWriter.WriteStartElement("name");//<name>
                xmlWriter.WriteValue(name);//[name]
                xmlWriter.WriteEndElement();//</name>
                xmlWriter.WriteStartElement("contents");//contents>
                xmlWriter.WriteValue(cell.Contents);//[contents]
                xmlWriter.WriteEndElement();//</contents>
                xmlWriter.WriteEndElement();//</cell>
            }
            xmlWriter.WriteEndElement();//</spreadsheet>
            xmlWriter.WriteEndDocument();
        }

        public override object GetCellValue(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? cell) ? cell.Value : "";
        }

        public override object GetCellContents(string name)
        {

            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.Contents : "";
        }


        public override IList<string> SetContentsOfCell(string name, string content)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            var deps = SetCellContents(name, new Formula(content));
            foreach (var n in deps)
                if (cells.TryGetValue(n, out ICell? cell))
                    cell.Compute();
            return deps;
        }
    }
}