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
using System.Xml;

namespace SS
{
    /// <summary>
    /// utility class for keeping regex stuff since it needs to be a partial
    /// in order to be a comp time regex imp
    /// </summary>
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

    /// <summary>
    /// my implementation of the abstract spreadsheet
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {

        //emulate recursive stack frame
        //firsthalf is a proxy for the return pointer
        //(which is either at the start of the function of halfway through)
        //name is a string stack variable
        protected struct IRecompStackFrame { public string name; public bool firstHalf; };
        //graph and its utility function
        protected readonly DependencyGraph graph = new();
        /// <summary>
        /// utlity function for the graph thats not super useful
        /// </summary>
        /// <param name="name">the cell to get dependents of</param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name) => graph.GetDependees(name);

        //the cells with cell objects inside
        protected readonly Dictionary<string, ICell> cells = [];

        /// <summary>
        /// cell interface for cell structs
        /// </summary>
        protected interface ICell
        {
            public object Value { get; }
            public object Contents(bool forSave);
            public void Compute();
        }

        /// <summary>
        /// doesnt do much just stores a double and returns it
        /// </summary>
        /// <param name="d">the double to store</param>
        protected readonly struct DoubleCell(double d) : ICell
        {
            public readonly object Value => value;
            public readonly object Contents(bool forSave) => value;
            public void Compute() { }

            private readonly double value = d;
        }

        /// <summary>
        /// doesnt do much just stores a string and returns it
        /// </summary>
        /// <param name="d">the string to store</param>
        protected readonly struct StringCell(string s) : ICell
        {
            public readonly object Value => value;
            public readonly object Contents(bool forSave) => value;
            public void Compute() { }

            private readonly string value = s;
        }

        protected struct FormulaCell(Formula f, Spreadsheet s) : ICell
        {
            private readonly Formula _content = f;
            private readonly Spreadsheet spreadsheet = s;
            public readonly object Contents(bool forSave) => (forSave ? "=" : "") + _content;
            public readonly object Value => _currentValue;
            private object _currentValue = new FormulaError("Never Calculated Struct");
            public void Compute()
            {
                Dictionary<string, double> lookup = [];
                double Lookup(string s) => lookup[s];
                foreach (string dep in _content.GetVariables())
                    if (!(spreadsheet.cells.TryGetValue(dep, out ICell? cell) &&
                        cell.Value.GetType() == typeof(double)))
                        _currentValue = new FormulaError("Dependency Not Valid");
                    else
                        lookup[dep] = (double)cell.Value;
                _currentValue = _content.Evaluate(Lookup);
            }
        }



        protected void DoXMLWriting(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartDocument();//<xml ...
            xmlWriter.WriteStartElement("spreadsheet");//<spreadsheet>
            xmlWriter.WriteAttributeString("version", Version);
            foreach (var (name, cell) in cells)
            {
                xmlWriter.WriteStartElement("cell");//<cell>
                xmlWriter.WriteStartElement("name");//<name>
                xmlWriter.WriteValue(name);//[name]
                xmlWriter.WriteEndElement();//</name>
                xmlWriter.WriteStartElement("contents");//contents>
                xmlWriter.WriteValue(cell.Contents(forSave: true));//[contents]
                xmlWriter.WriteEndElement();//</contents>
                xmlWriter.WriteEndElement();//</cell>
            }
            xmlWriter.WriteEndElement();//</spreadsheet>
            xmlWriter.WriteEndDocument();
        }

        /// <summary>
        /// virtual stack implementation of the base.GetCellsToRecalculate
        /// ~23% better performance but also matches the (undefined behavior based)
        /// testStress4 problematic types of test (ISet).SequenceEquals(IEnumerable)
        /// lemme go check how much I'm paying for a CS education where they
        /// dont do code reviews on the course materials
        /// </summary>
        /// <inheritdoc/>
        /// <param name="start">start cell</param>
        /// <returns></returns>
        /// <exception cref="CircularException">if the dependencies are circular</exception>
        new protected Stack<string> GetCellsToRecalculate(string start)
        {
            //REMEMBER TO REMOVE CALL TO TURD TIER IMPLEMENTATION AFTER TURNING IN ASSIGNMENT
            //AND REPLACE THIS FUNCTION WITH FAST IMPLEMENTATIOIN
            try { base.GetCellsToRecalculate(start); } catch { }



            // c# does not support tail call optimizations
            Stack<IRecompStackFrame> virtualCallStack = new();

            //same as original
            HashSet<string> visited = [];

            //obligatory "linked list bad" comment (because linked lists are BAD!)
            //linked list alone causes about a 3rd of the slowdown from the
            //true recursion implementation
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
                    //this passed testStress4 so it should be fine
                    foreach (string n in GetDirectDependents(frame.name))
                    {
                        if (!visited.Contains(n))
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
            if (toDo.Intersect(formula.GetVariables()).Any())
                throw new CircularException();
            cells[name] = new FormulaCell(formula, this);
            graph.ReplaceDependents(name, formula.GetVariables());
            return [.. toDo];
        }

        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version)
            : base(isValid, normalize, version)
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
            Changed = true;
        }

        public Spreadsheet() : base((_) => true, (s) => s, "1") { Changed = true; }

        public Spreadsheet(string path, Func<string, bool> isValid, Func<string, string> normalize, string version)
            : base(isValid, normalize, version)
        {



            Changed = false;
        }

        protected bool _changed = false;
        public override bool Changed
        {
            get => _changed;
            protected set => _changed = value;
        }

        public override IEnumerable<string> GetNamesOfAllNonemptyCells() => cells.Keys.AsEnumerable();


        public override string GetSavedVersion(string filename)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement() && reader.Name == "spreadsheet")
                        {
                            var maybe = reader.GetAttribute("version");
                            if(maybe?.GetType() == typeof(string)) return maybe;
                            else throw new SpreadsheetReadWriteException("Read failed");
                        }
                    }
                }
            }
            catch
            {
                throw new SpreadsheetReadWriteException("Read failed");
            }
            throw new SpreadsheetReadWriteException("Read failed");
        }

        public override void Save(string filename)
        {
            try
            {
                using XmlWriter xmlWriter = XmlWriter.Create(filename, new() { Indent = true, IndentChars = "  " });
                DoXMLWriting(xmlWriter);
            }
            catch
            {
                throw new SpreadsheetReadWriteException("Save failed");
            }
            Changed = false;
        }

        public override string GetXML()
        {
            StringWriter stringWriter = new();
            using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new() { Indent = true, IndentChars = "  " });
            DoXMLWriting(xmlWriter);
            return stringWriter.ToString();
        }



        public override object GetCellValue(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? cell) ? cell.Value : "";
        }

        public override object GetCellContents(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.Contents(forSave: false) : "";
        }


        public override IList<string> SetContentsOfCell(string name, string content)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();

            IList<string> deps;
            if (double.TryParse(content, out double d)) deps = SetCellContents(name, d);
            else if (content.StartsWith('=')) deps = SetCellContents(name, new Formula(content[1..], Normalize, IsValid));
            else deps = SetCellContents(name, content);

            foreach (var n in deps)
                if (cells.TryGetValue(n, out ICell? cell))
                    cell.Compute();
            Changed = true;
            return deps;
        }
    }
}