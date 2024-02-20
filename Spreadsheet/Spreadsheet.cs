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
using System.Linq;
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
        [GeneratedRegex(@"^[a-zA-Z][a-zA-Z\d]*$", options:
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking)]
        private static partial Regex _validName();
        private static readonly Regex validName = _validName();
        /// <summary>
        /// checks if a cell name is invalid
        /// </summary>
        /// <param name="s">the cell name to chekc</param>
        /// <returns></returns>
        public static bool IsInvalidName(string s) => !validName.IsMatch(s);


        [GeneratedRegex(@"^$", options:
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.NonBacktracking)]
        private static partial Regex _nothing();
        private static readonly Regex nothing = _nothing();
        /// <summary>
        /// checks if a string is empty
        /// </summary>
        /// <param name="s">the string that could be empty</param>
        /// <returns></returns>
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


        // whether the spreadsheet has changed
        protected bool _changed = false;


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
        protected interface ICell : IEquatable<ICell>
        {
            public object Value { get; }
            public object Contents(bool forSave);
            public void Compute();
            public object CompItem { get; }
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
            public object CompItem => Value;
            public bool Equals(ICell? other) =>
                (other?.GetType() == GetType()) && (other.CompItem == CompItem);

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
            public object CompItem => Value;
            public bool Equals(ICell? other) =>
                (other?.GetType() == GetType()) && (other.CompItem == CompItem);

            private readonly string value = s;
        }

        /// <summary>
        /// a cell to store formulas
        /// </summary>
        /// <param name="f">the formula</param>
        /// <param name="s">a refernce to the spreadsheet to access other cells</param>
        protected struct FormulaCell(Formula f, Spreadsheet s) : ICell
        {
            public readonly object Contents(bool forSave) => (forSave ? "=" : "") + _content;
            public readonly object Value => _currentValue;
            private object _currentValue = new FormulaError("Never Calculated Struct");
            public void Compute()
            {
                var vars = _content.GetVariables();
                foreach (var dep in vars)
                {
                    if (!spreadsheet.cells.TryGetValue(dep, out ICell? cell) || cell.Value.GetType() != typeof(double))
                    {
                        _currentValue = new FormulaError("Dependency Not Valid");
                        return;
                    }
                }
                try
                {
                    _currentValue = _content.Evaluate(Lookup);
                }
                catch
                {
                    _currentValue = new FormulaError("Dependency Not Valid");
                }
            }
            public readonly object CompItem => _content;
            public readonly bool Equals(ICell? other) =>
                (other?.GetType() == GetType()) && (other.CompItem == CompItem);

            private readonly Formula _content = f;
            private readonly Spreadsheet spreadsheet = s;
            private readonly double Lookup(string s)
            {
                //ulong index = PreHash(s);
                ICell cell = spreadsheet.cells[s];
                double val = (double)cell.Value;
                return val;
            }
        }


        /// <summary>
        /// a function to write xml to and xml writer
        /// </summary>
        /// <param name="xmlWriter">the writer to write this sheet to</param>
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


        protected static readonly Stack<IRecompStackFrame> virtualCallStack = new();
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
        new protected IEnumerable<string> GetCellsToRecalculate(string start)
        {
            HashSet<string> visited = [];
            string dep;
            //obligatory "linked list bad" comment (because linked lists are BAD!)
            //linked list alone causes about a 3rd of the slowdown from the
            //true recursion implementation
            Stack<string> changed = new();
            // c# does not support tail call optimizations

            //same as original
            //"Call" Visit(start...)
            IRecompStackFrame frame = new() { name = start, firstHalf = true };
            lock (virtualCallStack)
            {
                do
                {
                    if (frame.firstHalf)//ret pops either &Visit or &Visit + K
                    {
                        visited.Add(frame.name);
                        frame.firstHalf = false;
                        virtualCallStack.Push(frame);
                        foreach (string n in GetDirectDependents(frame.name))
                        {
                            dep = n;
                            if (!visited.Contains(dep))
                            {
                                virtualCallStack.Push(new() { name = dep, firstHalf = true });
                            }
                        }
                    }
                    else
                    {
                        changed.Push(frame.name);
                    }
                } while (virtualCallStack.TryPop(out frame));
            }
            return changed;//.Select(UnPreHash);
        }

        /// <summary>
        /// set a cell to a string value
        /// </summary>
        /// <param name="name">the name of the cell (assumed to be valid)</param>
        /// <param name="text">the string content</param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, string text)
        {
            //should be empty but whatever
            var toDo = GetCellsToRecalculate(name);

            //store only if its not empty
            if (!Utility.IsNothing(text))
            {
                cells[name] = new StringCell(text);
                graph.ReplaceDependents(name, []);
            }
            return toDo.ToList();
        }

        /// <summary>
        /// set a cell to a double
        /// </summary>
        /// <param name="name">the name of the cell (assumed to be valid)</param>
        /// <param name="number">the double value to store</param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, double number)
        {
            var toDo = GetCellsToRecalculate(name);

            //if its a new cell do recalulations
            ICell newCell = new DoubleCell(number);
            if (!(cells.TryGetValue(name, out ICell? oldCell) && (newCell == oldCell)))
            {
                cells[name] = newCell;
                graph.ReplaceDependents(name, []);
                newCell.Compute();
                if (newCell.Value.GetType() == typeof(double))
                    foreach (var n in toDo)
                        if (cells.TryGetValue(n, out ICell? cell))
                            cell.Compute();
            }
            return toDo.ToList();
        }

        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            //check if depends on its own vars
            if (formula.GetVariables().Contains(name)) throw new CircularException();
            //get recursive deps
            var toDo = GetCellsToRecalculate(name);
            //check if the recursive deps include its own deps
            if (toDo.Intersect(formula.GetVariables()).Any()) throw new CircularException();

            //if its a new cell do recalculations
            ICell newCell = new FormulaCell(formula, this);
            if (!(cells.TryGetValue(name, out ICell? oldCell) && (newCell == oldCell)))
            {
                cells[name] = newCell;
                graph.ReplaceDependents(name, formula.GetVariables());
                newCell.Compute();
                if (newCell.Value.GetType() == typeof(double))
                    foreach (var n in toDo)
                        if (cells.TryGetValue(n, out ICell? cell))
                            cell.Compute();
            }
            return toDo.ToList();
        }

        /// <summary>
        /// constructor for a new unsaved spreadsheet
        /// </summary>
        /// <param name="isValid">determines if a var is valid</param>
        /// <param name="normalize">normalizes the cell names</param>
        /// <param name="version">the version of this spreadsheet</param>
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version)
            : base(isValid, normalize, version) { Changed = true; }

        /// <summary>
        /// a constructor with no parameters that puts default values in
        /// </summary>
        public Spreadsheet() : base((_) => true, (s) => s, "1") { Changed = true; }

        /// <summary>
        /// a constructor that reads from a file and stores it in this spreadsheet
        /// </summary>
        /// <param name="path">path to the file</param>
        /// <param name="isValid">checks if var is valid</param>
        /// <param name="normalize">normalize name of var</param>
        /// <param name="version">the version string</param>
        public Spreadsheet(string path, Func<string, bool> isValid, Func<string, string> normalize, string version)
            : base(isValid, normalize, version)
        {
            Changed = false;
            string name;
            string content;
            try
            {
                using var reader = XmlReader.Create(path);
                while (reader.Read())
                {
                    if (reader.IsStartElement() && reader.Name == "cell")
                    {
                        name = "";
                        content = "";
                        while (reader.Read())
                            switch (reader.NodeType)
                            {
                                case XmlNodeType.EndElement:
                                    if (reader.Name == "cell") goto exitloop; //AAAAAH CALL THE POILCE ITS A GOTO
                                    break;
                                case XmlNodeType.Element:
                                    if (reader.Name == "name" && reader.Read())
                                        name = reader.Value.Trim();
                                    else if (reader.Name == "contents" && reader.Read())
                                        content = reader.Value.Trim();
                                    break;
                                default:
                                    continue;
                            }
                        exitloop:
                        if (!name.Equals("") && !content.Equals("")) SetContentsOfCell(name, content);
                    }
                }
            }
            catch
            {
                throw new SpreadsheetReadWriteException("Read failed");
            }
        }

        public override bool Changed
        {
            get => _changed;
            protected set => _changed = value;
        }

        /// <summary>
        /// get all the cells youve populated
        /// </summary>
        /// <inheritdoc/>
        /// <returns></returns>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells() => cells.Keys;//.Select(UnPreHash);

        /// <summary>
        /// get the string version from the xml file with the given path
        /// </summary>
        /// <inheritdoc/>
        /// <param name="filename">the file to check</param>
        /// <returns>the string it found</returns>
        /// <exception cref="SpreadsheetReadWriteException">if something goes wrong with reading</exception>
        public override string GetSavedVersion(string filename)
        {
            string? maybeVersion;
            try
            {
                using XmlReader reader = XmlReader.Create(filename);
                while (reader.Read())
                    if (reader.IsStartElement() && reader.Name == "spreadsheet" &&
                        (maybeVersion = reader.GetAttribute("version"))?.GetType() == typeof(string)) return maybeVersion;
                    else throw new SpreadsheetReadWriteException("Read failed");
            }
            catch
            {
                throw new SpreadsheetReadWriteException("Read failed");
            }
            throw new SpreadsheetReadWriteException("Read failed");
        }

        /// <summary>
        /// saves a file to a path
        /// </summary>
        /// <inheritdoc/>
        /// <param name="filename"></param>
        /// <exception cref="SpreadsheetReadWriteException">if the file cant be saved for some reason</exception>
        public override void Save(string filename)
        {
            Changed = false;
            try
            {
                using var xmlWriter = XmlWriter.Create(filename, new() { Indent = true, IndentChars = "  " });
                DoXMLWriting(xmlWriter);
            }
            catch
            {
                throw new SpreadsheetReadWriteException("Save failed");
            }
        }

        /// <summary>
        /// gets xml string version of this spreedsheet
        /// </summary>
        /// <inheritdoc/>
        /// <returns></returns>
        public override string GetXML()
        {
            StringWriter stringWriter = new();
            DoXMLWriting(XmlWriter.Create(stringWriter, new() { Indent = true, IndentChars = "  " }));
            return stringWriter.ToString();
        }


        /// <summary>
        /// tries to get a cell value
        /// </summary>
        /// <inheritdoc/>
        /// <param name="name">the cell name to try to get a value from</param>
        /// <returns></returns>
        /// <exception cref="InvalidNameException">if the name is a bad name</exception>
        public override object GetCellValue(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? cell) ? cell.Value : "";
        }

        /// <summary>
        /// tries to get a cell contents
        /// </summary>
        /// <inheritdoc/>
        /// <param name="name">the cell name to try to get a contents from</param>
        /// <returns></returns>
        /// <exception cref="InvalidNameException">if the name is a bad name</exception>
        public override object GetCellContents(string name)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            return cells.TryGetValue(name, out ICell? value) ? value.Contents(forSave: false) : "";
        }

        /// <summary>
        /// set the content of the cell and return recusive deps
        /// </summary>
        /// <inheritdoc/>
        /// <param name="name">name of cell to set</param>
        /// <param name="content">content to set the cell to</param>
        /// <returns>the dependees of this cell</returns>
        /// <exception cref="InvalidNameException">if the cell name is not a good name</exception>
        public override IList<string> SetContentsOfCell(string name, string content)
        {
            if (Utility.IsInvalidName(name)) throw new InvalidNameException();
            Changed = true;
            return double.TryParse(content, out double d) ? SetCellContents(name, d) :
                    content.StartsWith('=') ? SetCellContents(name, new Formula(content[1..], Normalize, IsValid)) :
                    SetCellContents(name, content);
        }
    }
}