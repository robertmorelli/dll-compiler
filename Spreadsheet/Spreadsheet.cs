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


using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml;
using SpreadsheetUtilities;

namespace SS;

/// <summary>
///     Utility class for keeping regex stuff since it needs to be a partial
///     in order to be a comp time regex imp
/// </summary>
internal static partial class Utility
{
    private static readonly Regex ValidName = _validName();
    private static readonly Regex Nothing = _nothing();

    [GeneratedRegex(@"^\w[\w\d]*$", RegexOptions.IgnorePatternWhitespace |
                                    RegexOptions.NonBacktracking)]
    private static partial Regex _validName();

    /// <summary>
    ///     checks if a cell name is invalid
    /// </summary>
    /// <param name="s">the cell name to check</param>
    /// <returns></returns>
    public static bool IsInvalidName(string s)
    {
        return !ValidName.IsMatch(s);
    }


    [GeneratedRegex(@"^$", RegexOptions.IgnorePatternWhitespace |
                           RegexOptions.NonBacktracking)]
    private static partial Regex _nothing();

    /// <summary>
    ///     checks if a string is empty
    /// </summary>
    /// <param name="s">the string that could be empty</param>
    /// <returns></returns>
    public static bool IsNothing(string s)
    {
        return Nothing.IsMatch(s);
    }
}

/// <summary>
///     my implementation of the abstract spreadsheet
/// </summary>
public class Spreadsheet : AbstractSpreadsheet
{
    //graph and its utility function
    private readonly DependencyGraph _graph = new();

    //the cells with cell objects inside
    protected readonly Dictionary<string, ICell> cells = [];

    // whether the spreadsheet has changed

    /// <summary>
    ///     constructor for a new unsaved spreadsheet
    /// </summary>
    /// <param name="isValid">determines if a var is valid</param>
    /// <param name="normalize">normalizes the cell names</param>
    /// <param name="version">the version of this spreadsheet</param>
    public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor with no parameters that puts default values in
    /// </summary>
    public Spreadsheet() : base(_ => true, s => s, "1")
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor that reads from a file and stores it in this spreadsheet
    /// </summary>
    /// <param name="path">path to the file</param>
    /// <param name="isValid">checks if var is valid</param>
    /// <param name="normalize">normalize name of var</param>
    /// <param name="version">the version string</param>
    public Spreadsheet(string path, Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
        try
        {
            using var reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (!(reader.IsStartElement() && reader.Name == "cell")) continue;
                var name = "";
                var content = "";
                while (reader.Read())
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.EndElement:
                            if (reader.Name == "cell")
                                goto
                                    exitLoop; //AAH CALL THE POLICE ITS A GOTO (no break loop in switch and no labeled break)
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

                exitLoop:
                if (!name.Equals("") && !content.Equals("")) SetContentsOfCell(name, content);
            }
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }
    }

    public override bool Changed { get; protected set; }

    /// <summary>
    ///     utility function for the graph that's not super useful
    /// </summary>
    /// <param name="name">the cell to get dependents of</param>
    /// <returns></returns>
    protected override IEnumerable<string> GetDirectDependents(string name)
    {
        return _graph.GetDependees(name);
    }


    /// <summary>
    ///     a function to write xml to and xml writer
    /// </summary>
    /// <param name="xmlWriter">the writer to write this sheet to</param>
    private void DoXmlWriting(XmlWriter xmlWriter)
    {
        xmlWriter.WriteStartDocument(); //<xml ...
        xmlWriter.WriteStartElement("spreadsheet"); //<spreadsheet>
        xmlWriter.WriteAttributeString("version", Version);
        foreach (var (name, cell) in cells)
        {
            xmlWriter.WriteStartElement("cell"); //<cell>
            xmlWriter.WriteStartElement("name"); //<name>
            xmlWriter.WriteValue(name); //[name]
            xmlWriter.WriteEndElement(); //</name>
            xmlWriter.WriteStartElement("contents"); //contents>
            xmlWriter.WriteValue(cell.Contents(true)); //[contents]
            xmlWriter.WriteEndElement(); //</contents>
            xmlWriter.WriteEndElement(); //</cell>
        }

        xmlWriter.WriteEndElement(); //</spreadsheet>
        xmlWriter.WriteEndDocument();
    }


    /// <summary>
    ///     virtual stack implementation of the base.GetCellsToRecalculate
    ///     ~23% better performance but also matches the (undefined behavior based)
    ///     testStress4 problematic types of test (ISet).SequenceEquals(IEnumerable)
    ///     lemme go check how much I'm paying for a CS education where they
    ///     dont do code reviews on the course materials
    /// </summary>
    /// <param name="start">start cell</param>
    /// <returns></returns>
    private new RecalculatedCellList GetCellsToRecalculate(string start)
    {
        return new RecalculatedCellList(start, this);
    }

    /// <summary>
    ///     set a cell to a string value
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="text">the string content</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, string text)
    {
        //should be empty but whatever
        IList<string> deps = GetCellsToRecalculate(name);

        cells.Remove(name);
        //store only if its not empty
        ICell newCell = new StringCell(text);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     set a cell to a double
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="number">the double value to store</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, double number)
    {
        IList<string> deps = GetCellsToRecalculate(name);

        //if its a new cell do recalculations
        ICell newCell = new DoubleCell(number);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    protected override IList<string> SetCellContents(string name, Formula formula)
    {
        //check if depends on its own vars
        if (formula.GetVariables().Contains(name)) throw new CircularException();
        //get recursive deps
        IList<string> deps = GetCellsToRecalculate(name);
        //check if the recursive deps include its own deps
        if (deps.Intersect(formula.GetVariables()).Any()) throw new CircularException();

        //if its a new cell do recalculations
        ICell newCell = new FormulaCell(formula, this);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, formula.GetVariables());
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     get all the cells you've populated
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override IEnumerable<string> GetNamesOfAllNonemptyCells()
    {
        return cells.Keys;
        //.Select(UnPreHash);
    }

    /// <summary>
    ///     get the string version from the xml file with the given path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename">the file to check</param>
    /// <returns>the string it found</returns>
    /// <exception cref="SpreadsheetReadWriteException">if something goes wrong with reading</exception>
    public override string GetSavedVersion(string filename)
    {
        try
        {
            string? maybeVersion;
            using var reader = XmlReader.Create(filename);
            while (reader.Read())
                if (reader.IsStartElement() && reader.Name == "spreadsheet" &&
                    (maybeVersion = reader.GetAttribute("version"))?.GetType() == typeof(string))
                    return maybeVersion;
                else throw new SpreadsheetReadWriteException("Read failed");
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }

        throw new SpreadsheetReadWriteException("Read failed");
    }

    /// <summary>
    ///     saves a file to a path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename"></param>
    /// <exception cref="SpreadsheetReadWriteException">if the file cant be saved for some reason</exception>
    public override void Save(string filename)
    {
        Changed = false;
        try
        {
            using var xmlWriter =
                XmlWriter.Create(filename, new XmlWriterSettings { Indent = true, IndentChars = "  " });
            DoXmlWriting(xmlWriter);
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Save failed");
        }
    }

    /// <summary>
    ///     gets xml string version of this spreadsheet
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override string GetXML()
    {
        StringWriter stringWriter = new();
        DoXmlWriting(
            XmlWriter.Create(
                stringWriter,
                new XmlWriterSettings { Indent = true, IndentChars = "  " }));
        return stringWriter.ToString();
    }


    /// <summary>
    ///     tries to get a cell value
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a value from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellValue(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var cell) ? cell.Value : "";
    }

    /// <summary>
    ///     tries to get a cell contents
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a contents from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellContents(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(false) : "";
    }

    public object GetCellContents(string name, bool better)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(better) : "";
    }

    /// <summary>
    ///     set the content of the cell and return recursive deps
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">name of cell to set</param>
    /// <param name="content">content to set the cell to</param>
    /// <returns>the dependees of this cell</returns>
    /// <exception cref="InvalidNameException">if the cell name is not a good name</exception>
    public override IList<string> SetContentsOfCell(string name, string content)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        Changed = true;
        return double.TryParse(content, out var d) ? SetCellContents(name, d) :
            content.StartsWith('=') ? SetCellContents(name, new Formula(content[1..], Normalize, IsValid)) :
            SetCellContents(name, content);
    }

    public string Compile(string name)
    {
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name + "Assembly"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(name + "Module");
        var typeBuilder =
            moduleBuilder.DefineType("sheetSpace.sheetLibrary", TypeAttributes.Public | TypeAttributes.Class);
        var fields = new Dictionary<string, FieldBuilder>(); //field references to create setters/getters
        var computeFunctions = new Dictionary<string, MethodBuilder>(); //field references to create setters

        //define all fields and getters
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName].Value is not double) continue;
            var fb = typeBuilder.DefineField("_" + cellName, typeof(double), FieldAttributes.Private);
            fields[cellName] = fb;
            var getterMethod = typeBuilder.DefineMethod("Get_" + cellName, MethodAttributes.Public, typeof(double),
                Type.EmptyTypes);
            var getterIl = getterMethod.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fields[cellName]);
            getterIl.Emit(OpCodes.Ret);
        }

        //define constructor
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);
        var ctorIl = ctor.GetILGenerator();
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName].Value is not double d) continue;
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldc_R8, d);
            ctorIl.Emit(OpCodes.Stfld, fields[cellName]);
        }

        ctorIl.Emit(OpCodes.Ret);

        //define all compute formula functions
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName] is DoubleCell ||
                cells[cellName].Value is not double) continue;
            var computeMethod = typeBuilder.DefineMethod("Compute_" + cellName, MethodAttributes.Private, typeof(void),
                Type.EmptyTypes);
            var computeIl = computeMethod.GetILGenerator();
            computeIl.Emit(OpCodes.Ldarg_0);
            ((FormulaCell)cells[cellName]).Compile(computeIl, fields);
            computeIl.Emit(OpCodes.Stfld, fields[cellName]);
            computeIl.Emit(OpCodes.Ret);
            computeFunctions[cellName] = computeMethod;
        }

        //define all field setters to compute relevant fields
        foreach (var cellName in cells.Keys)
        {
            IEnumerable<string> deps = GetCellsToRecalculate(cellName);
            if (cells[cellName] is StringCell || cells[cellName] is FormulaCell ||
                cells[cellName].Value is not double) continue;
            var setterMethod = typeBuilder.DefineMethod("Put_" + cellName, MethodAttributes.Public, typeof(void),
                [typeof(double)]);
            var setCellMsilGenerator = setterMethod.GetILGenerator();
            setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
            setCellMsilGenerator.Emit(OpCodes.Ldarg_1);
            setCellMsilGenerator.Emit(OpCodes.Stfld, fields[cellName]);

            // Dependency recalculation:
            foreach (var depCell in deps)
            {
                if (!computeFunctions.TryGetValue(depCell, out var b)) continue;
                setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
                setCellMsilGenerator.Emit(OpCodes.Callvirt, b);
            }

            setCellMsilGenerator.Emit(OpCodes.Ret);
        }

        typeBuilder.CreateType();
        var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".dll");
        File.Delete(fullPath);
        //assemblyBuilder.Save(fullPath);
        return fullPath;
    }

    /// <summary>
    ///     cell interface for cell structs
    /// </summary>
    protected interface ICell : IEquatable<ICell>
    {
        public object Value { get; }
        public object CompItem { get; }
        public object Contents(bool forSave);
        public void Compute();
    }

    /// <summary>
    ///     doesnt do much just stores a double and returns it
    /// </summary>
    /// <param name="d">the double to store</param>
    private readonly struct DoubleCell(double d) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly double _value = d;
    }

    /// <summary>
    ///     doesnt do much just stores a string and returns it
    /// </summary>
    /// <param name="s">the string to store</param>
    private readonly struct StringCell(string s) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly string _value = s;
    }

    /// <summary>
    ///     a cell to store formulas
    /// </summary>
    /// <param name="f">the formula</param>
    /// <param name="s">a reference to the spreadsheet to access other cells</param>
    private struct FormulaCell(Formula f, Spreadsheet s) : ICell
    {
        public readonly object Contents(bool forSave)
        {
            return (forSave ? "=" : "") + _content;
        }

        public object Value { get; private set; } = new FormulaError("Never Calculated Struct");

        public void Compute()
        {
            var vars = _content.GetVariables();
            foreach (var dep in vars)
                if (!_spreadsheet.cells.TryGetValue(dep, out var cell) || cell.Value.GetType() != typeof(double))
                {
                    Value = new FormulaError("Dependency Not Valid");
                    return;
                }

            try
            {
                Value = _content.Evaluate(Lookup);
            }
            catch (Exception e)
            {
                Value = new FormulaError("Dependency Not Valid " + e.Message);
            }
        }

        public readonly object CompItem => _content;

        public readonly bool Equals(ICell? other)
        {
            return other is FormulaCell && other.CompItem == CompItem;
        }

        private readonly Formula _content = f;
        private readonly Spreadsheet _spreadsheet = s;

        private readonly double Lookup(string s)
        {
            var cell = _spreadsheet.cells[s];
            var val = (double)cell.Value;
            return val;
        }

        public void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields)
        {
            _content.Compile(gen, fields);
        }
    }

    /// <summary>
    ///     A dep list that only does stuff if its ever used
    /// </summary>
    /// <param name="name">cell name</param>
    /// <param name="s">the spreadsheet reference</param>
    private struct RecalculatedCellList(string name, Spreadsheet s) : IList<string>
    {
        //emulate recursive stack frame
        //first half is a proxy for the return pointer
        //(which is either at the start of the function of halfway through)
        //name is a string stack variable
        private struct RecomputeStackFrame
        {
            public string name;
            public bool firstHalf;
        }

        private static readonly Stack<RecomputeStackFrame> VirtualCallStack = new();
        private IList<string>? _actualList;
        private readonly Spreadsheet _spreadsheet = s;
        private readonly string _start = name;

        private IList<string> EnsureList()
        {
            return _actualList ??= GetList();
        }

        private IList<string> GetList()
        {
            HashSet<string> visited = [];
            //obligatory "linked list bad" comment (because linked lists are BAD!)
            //linked list alone causes about a 3rd of the slowdown from the
            //true recursion implementation
            Stack<string> changed = new();
            // c# does not support tail call optimizations

            //same as original
            //"Call" Visit(start...)
            RecomputeStackFrame frame = new() { name = _start, firstHalf = true };
            lock (VirtualCallStack)
            {
                do
                {
                    if (frame.firstHalf) //ret pops either &Visit or &Visit + K
                    {
                        visited.Add(frame.name);
                        frame.firstHalf = false;
                        VirtualCallStack.Push(frame);
                        foreach (var dep in _spreadsheet.GetDirectDependents(frame.name))
                            if (!visited.Contains(dep))
                                VirtualCallStack.Push(new RecomputeStackFrame { name = dep, firstHalf = true });
                    }
                    else
                    {
                        changed.Push(frame.name);
                    }
                } while (VirtualCallStack.TryPop(out frame));
            }

            return [.. changed];
        }

        public string this[int index]
        {
            get => EnsureList()[index];
            set => EnsureList()[index] = value;
        }

        public int Count => EnsureList().Count;
        public readonly bool IsReadOnly => true;

        public void Add(string item)
        {
            EnsureList().Add(item);
        }

        public void Clear()
        {
            EnsureList().Clear();
        }

        public bool Contains(string item)
        {
            return EnsureList().Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            EnsureList().CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return EnsureList().GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return EnsureList().IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            EnsureList().Insert(index, item);
        }

        public bool Remove(string item)
        {
            return EnsureList().Remove(item);
        }

        public void RemoveAt(int index)
        {
            EnsureList().RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)EnsureList()).GetEnumerator();
        }
    }
}/// <summary>
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


using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml;
using SpreadsheetUtilities;

namespace SS;

/// <summary>
///     Utility class for keeping regex stuff since it needs to be a partial
///     in order to be a comp time regex imp
/// </summary>
internal static partial class Utility
{
    private static readonly Regex ValidName = _validName();
    private static readonly Regex Nothing = _nothing();

    [GeneratedRegex(@"^\w[\w\d]*$", RegexOptions.IgnorePatternWhitespace |
                                    RegexOptions.NonBacktracking)]
    private static partial Regex _validName();

    /// <summary>
    ///     checks if a cell name is invalid
    /// </summary>
    /// <param name="s">the cell name to check</param>
    /// <returns></returns>
    public static bool IsInvalidName(string s)
    {
        return !ValidName.IsMatch(s);
    }


    [GeneratedRegex(@"^$", RegexOptions.IgnorePatternWhitespace |
                           RegexOptions.NonBacktracking)]
    private static partial Regex _nothing();

    /// <summary>
    ///     checks if a string is empty
    /// </summary>
    /// <param name="s">the string that could be empty</param>
    /// <returns></returns>
    public static bool IsNothing(string s)
    {
        return Nothing.IsMatch(s);
    }
}

/// <summary>
///     my implementation of the abstract spreadsheet
/// </summary>
public class Spreadsheet : AbstractSpreadsheet
{
    //graph and its utility function
    private readonly DependencyGraph _graph = new();

    //the cells with cell objects inside
    protected readonly Dictionary<string, ICell> cells = [];

    // whether the spreadsheet has changed

    /// <summary>
    ///     constructor for a new unsaved spreadsheet
    /// </summary>
    /// <param name="isValid">determines if a var is valid</param>
    /// <param name="normalize">normalizes the cell names</param>
    /// <param name="version">the version of this spreadsheet</param>
    public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor with no parameters that puts default values in
    /// </summary>
    public Spreadsheet() : base(_ => true, s => s, "1")
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor that reads from a file and stores it in this spreadsheet
    /// </summary>
    /// <param name="path">path to the file</param>
    /// <param name="isValid">checks if var is valid</param>
    /// <param name="normalize">normalize name of var</param>
    /// <param name="version">the version string</param>
    public Spreadsheet(string path, Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
        try
        {
            using var reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (!(reader.IsStartElement() && reader.Name == "cell")) continue;
                var name = "";
                var content = "";
                while (reader.Read())
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.EndElement:
                            if (reader.Name == "cell")
                                goto
                                    exitLoop; //AAH CALL THE POLICE ITS A GOTO (no break loop in switch and no labeled break)
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

                exitLoop:
                if (!name.Equals("") && !content.Equals("")) SetContentsOfCell(name, content);
            }
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }
    }

    public override bool Changed { get; protected set; }

    /// <summary>
    ///     utility function for the graph that's not super useful
    /// </summary>
    /// <param name="name">the cell to get dependents of</param>
    /// <returns></returns>
    protected override IEnumerable<string> GetDirectDependents(string name)
    {
        return _graph.GetDependees(name);
    }


    /// <summary>
    ///     a function to write xml to and xml writer
    /// </summary>
    /// <param name="xmlWriter">the writer to write this sheet to</param>
    private void DoXmlWriting(XmlWriter xmlWriter)
    {
        xmlWriter.WriteStartDocument(); //<xml ...
        xmlWriter.WriteStartElement("spreadsheet"); //<spreadsheet>
        xmlWriter.WriteAttributeString("version", Version);
        foreach (var (name, cell) in cells)
        {
            xmlWriter.WriteStartElement("cell"); //<cell>
            xmlWriter.WriteStartElement("name"); //<name>
            xmlWriter.WriteValue(name); //[name]
            xmlWriter.WriteEndElement(); //</name>
            xmlWriter.WriteStartElement("contents"); //contents>
            xmlWriter.WriteValue(cell.Contents(true)); //[contents]
            xmlWriter.WriteEndElement(); //</contents>
            xmlWriter.WriteEndElement(); //</cell>
        }

        xmlWriter.WriteEndElement(); //</spreadsheet>
        xmlWriter.WriteEndDocument();
    }


    /// <summary>
    ///     virtual stack implementation of the base.GetCellsToRecalculate
    ///     ~23% better performance but also matches the (undefined behavior based)
    ///     testStress4 problematic types of test (ISet).SequenceEquals(IEnumerable)
    ///     lemme go check how much I'm paying for a CS education where they
    ///     dont do code reviews on the course materials
    /// </summary>
    /// <param name="start">start cell</param>
    /// <returns></returns>
    private new RecalculatedCellList GetCellsToRecalculate(string start)
    {
        return new RecalculatedCellList(start, this);
    }

    /// <summary>
    ///     set a cell to a string value
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="text">the string content</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, string text)
    {
        //should be empty but whatever
        IList<string> deps = GetCellsToRecalculate(name);

        cells.Remove(name);
        //store only if its not empty
        ICell newCell = new StringCell(text);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     set a cell to a double
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="number">the double value to store</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, double number)
    {
        IList<string> deps = GetCellsToRecalculate(name);

        //if its a new cell do recalculations
        ICell newCell = new DoubleCell(number);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    protected override IList<string> SetCellContents(string name, Formula formula)
    {
        //check if depends on its own vars
        if (formula.GetVariables().Contains(name)) throw new CircularException();
        //get recursive deps
        IList<string> deps = GetCellsToRecalculate(name);
        //check if the recursive deps include its own deps
        if (deps.Intersect(formula.GetVariables()).Any()) throw new CircularException();

        //if its a new cell do recalculations
        ICell newCell = new FormulaCell(formula, this);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, formula.GetVariables());
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     get all the cells you've populated
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override IEnumerable<string> GetNamesOfAllNonemptyCells()
    {
        return cells.Keys;
        //.Select(UnPreHash);
    }

    /// <summary>
    ///     get the string version from the xml file with the given path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename">the file to check</param>
    /// <returns>the string it found</returns>
    /// <exception cref="SpreadsheetReadWriteException">if something goes wrong with reading</exception>
    public override string GetSavedVersion(string filename)
    {
        try
        {
            string? maybeVersion;
            using var reader = XmlReader.Create(filename);
            while (reader.Read())
                if (reader.IsStartElement() && reader.Name == "spreadsheet" &&
                    (maybeVersion = reader.GetAttribute("version"))?.GetType() == typeof(string))
                    return maybeVersion;
                else throw new SpreadsheetReadWriteException("Read failed");
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }

        throw new SpreadsheetReadWriteException("Read failed");
    }

    /// <summary>
    ///     saves a file to a path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename"></param>
    /// <exception cref="SpreadsheetReadWriteException">if the file cant be saved for some reason</exception>
    public override void Save(string filename)
    {
        Changed = false;
        try
        {
            using var xmlWriter =
                XmlWriter.Create(filename, new XmlWriterSettings { Indent = true, IndentChars = "  " });
            DoXmlWriting(xmlWriter);
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Save failed");
        }
    }

    /// <summary>
    ///     gets xml string version of this spreadsheet
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override string GetXML()
    {
        StringWriter stringWriter = new();
        DoXmlWriting(
            XmlWriter.Create(
                stringWriter,
                new XmlWriterSettings { Indent = true, IndentChars = "  " }));
        return stringWriter.ToString();
    }


    /// <summary>
    ///     tries to get a cell value
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a value from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellValue(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var cell) ? cell.Value : "";
    }

    /// <summary>
    ///     tries to get a cell contents
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a contents from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellContents(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(false) : "";
    }

    public object GetCellContents(string name, bool better)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(better) : "";
    }

    /// <summary>
    ///     set the content of the cell and return recursive deps
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">name of cell to set</param>
    /// <param name="content">content to set the cell to</param>
    /// <returns>the dependees of this cell</returns>
    /// <exception cref="InvalidNameException">if the cell name is not a good name</exception>
    public override IList<string> SetContentsOfCell(string name, string content)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        Changed = true;
        return double.TryParse(content, out var d) ? SetCellContents(name, d) :
            content.StartsWith('=') ? SetCellContents(name, new Formula(content[1..], Normalize, IsValid)) :
            SetCellContents(name, content);
    }

    public string Compile(string name)
    {
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name + "Assembly"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(name + "Module");
        var typeBuilder =
            moduleBuilder.DefineType("sheetSpace.sheetLibrary", TypeAttributes.Public | TypeAttributes.Class);
        var fields = new Dictionary<string, FieldBuilder>(); //field references to create setters/getters
        var computeFunctions = new Dictionary<string, MethodBuilder>(); //field references to create setters

        //define all fields and getters
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName].Value is not double) continue;
            var fb = typeBuilder.DefineField("_" + cellName, typeof(double), FieldAttributes.Private);
            fields[cellName] = fb;
            var getterMethod = typeBuilder.DefineMethod("Get_" + cellName, MethodAttributes.Public, typeof(double),
                Type.EmptyTypes);
            var getterIl = getterMethod.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fields[cellName]);
            getterIl.Emit(OpCodes.Ret);
        }

        //define constructor
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);
        var ctorIl = ctor.GetILGenerator();
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName].Value is not double d) continue;
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldc_R8, d);
            ctorIl.Emit(OpCodes.Stfld, fields[cellName]);
        }

        ctorIl.Emit(OpCodes.Ret);

        //define all compute formula functions
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName] is DoubleCell ||
                cells[cellName].Value is not double) continue;
            var computeMethod = typeBuilder.DefineMethod("Compute_" + cellName, MethodAttributes.Private, typeof(void),
                Type.EmptyTypes);
            var computeIl = computeMethod.GetILGenerator();
            computeIl.Emit(OpCodes.Ldarg_0);
            ((FormulaCell)cells[cellName]).Compile(computeIl, fields);
            computeIl.Emit(OpCodes.Stfld, fields[cellName]);
            computeIl.Emit(OpCodes.Ret);
            computeFunctions[cellName] = computeMethod;
        }

        //define all field setters to compute relevant fields
        foreach (var cellName in cells.Keys)
        {
            IEnumerable<string> deps = GetCellsToRecalculate(cellName);
            if (cells[cellName] is StringCell || cells[cellName] is FormulaCell ||
                cells[cellName].Value is not double) continue;
            var setterMethod = typeBuilder.DefineMethod("Put_" + cellName, MethodAttributes.Public, typeof(void),
                [typeof(double)]);
            var setCellMsilGenerator = setterMethod.GetILGenerator();
            setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
            setCellMsilGenerator.Emit(OpCodes.Ldarg_1);
            setCellMsilGenerator.Emit(OpCodes.Stfld, fields[cellName]);

            // Dependency recalculation:
            foreach (var depCell in deps)
            {
                if (!computeFunctions.TryGetValue(depCell, out var b)) continue;
                setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
                setCellMsilGenerator.Emit(OpCodes.Callvirt, b);
            }

            setCellMsilGenerator.Emit(OpCodes.Ret);
        }

        typeBuilder.CreateType();
        var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".dll");
        File.Delete(fullPath);
        //assemblyBuilder.Save(fullPath);
        return fullPath;
    }

    /// <summary>
    ///     cell interface for cell structs
    /// </summary>
    protected interface ICell : IEquatable<ICell>
    {
        public object Value { get; }
        public object CompItem { get; }
        public object Contents(bool forSave);
        public void Compute();
    }

    /// <summary>
    ///     doesnt do much just stores a double and returns it
    /// </summary>
    /// <param name="d">the double to store</param>
    private readonly struct DoubleCell(double d) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly double _value = d;
    }

    /// <summary>
    ///     doesnt do much just stores a string and returns it
    /// </summary>
    /// <param name="s">the string to store</param>
    private readonly struct StringCell(string s) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly string _value = s;
    }

    /// <summary>
    ///     a cell to store formulas
    /// </summary>
    /// <param name="f">the formula</param>
    /// <param name="s">a reference to the spreadsheet to access other cells</param>
    private struct FormulaCell(Formula f, Spreadsheet s) : ICell
    {
        public readonly object Contents(bool forSave)
        {
            return (forSave ? "=" : "") + _content;
        }

        public object Value { get; private set; } = new FormulaError("Never Calculated Struct");

        public void Compute()
        {
            var vars = _content.GetVariables();
            foreach (var dep in vars)
                if (!_spreadsheet.cells.TryGetValue(dep, out var cell) || cell.Value.GetType() != typeof(double))
                {
                    Value = new FormulaError("Dependency Not Valid");
                    return;
                }

            try
            {
                Value = _content.Evaluate(Lookup);
            }
            catch (Exception e)
            {
                Value = new FormulaError("Dependency Not Valid " + e.Message);
            }
        }

        public readonly object CompItem => _content;

        public readonly bool Equals(ICell? other)
        {
            return other is FormulaCell && other.CompItem == CompItem;
        }

        private readonly Formula _content = f;
        private readonly Spreadsheet _spreadsheet = s;

        private readonly double Lookup(string s)
        {
            var cell = _spreadsheet.cells[s];
            var val = (double)cell.Value;
            return val;
        }

        public void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields)
        {
            _content.Compile(gen, fields);
        }
    }

    /// <summary>
    ///     A dep list that only does stuff if its ever used
    /// </summary>
    /// <param name="name">cell name</param>
    /// <param name="s">the spreadsheet reference</param>
    private struct RecalculatedCellList(string name, Spreadsheet s) : IList<string>
    {
        //emulate recursive stack frame
        //first half is a proxy for the return pointer
        //(which is either at the start of the function of halfway through)
        //name is a string stack variable
        private struct RecomputeStackFrame
        {
            public string name;
            public bool firstHalf;
        }

        private static readonly Stack<RecomputeStackFrame> VirtualCallStack = new();
        private IList<string>? _actualList;
        private readonly Spreadsheet _spreadsheet = s;
        private readonly string _start = name;

        private IList<string> EnsureList()
        {
            return _actualList ??= GetList();
        }

        private IList<string> GetList()
        {
            HashSet<string> visited = [];
            //obligatory "linked list bad" comment (because linked lists are BAD!)
            //linked list alone causes about a 3rd of the slowdown from the
            //true recursion implementation
            Stack<string> changed = new();
            // c# does not support tail call optimizations

            //same as original
            //"Call" Visit(start...)
            RecomputeStackFrame frame = new() { name = _start, firstHalf = true };
            lock (VirtualCallStack)
            {
                do
                {
                    if (frame.firstHalf) //ret pops either &Visit or &Visit + K
                    {
                        visited.Add(frame.name);
                        frame.firstHalf = false;
                        VirtualCallStack.Push(frame);
                        foreach (var dep in _spreadsheet.GetDirectDependents(frame.name))
                            if (!visited.Contains(dep))
                                VirtualCallStack.Push(new RecomputeStackFrame { name = dep, firstHalf = true });
                    }
                    else
                    {
                        changed.Push(frame.name);
                    }
                } while (VirtualCallStack.TryPop(out frame));
            }

            return [.. changed];
        }

        public string this[int index]
        {
            get => EnsureList()[index];
            set => EnsureList()[index] = value;
        }

        public int Count => EnsureList().Count;
        public readonly bool IsReadOnly => true;

        public void Add(string item)
        {
            EnsureList().Add(item);
        }

        public void Clear()
        {
            EnsureList().Clear();
        }

        public bool Contains(string item)
        {
            return EnsureList().Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            EnsureList().CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return EnsureList().GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return EnsureList().IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            EnsureList().Insert(index, item);
        }

        public bool Remove(string item)
        {
            return EnsureList().Remove(item);
        }

        public void RemoveAt(int index)
        {
            EnsureList().RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)EnsureList()).GetEnumerator();
        }
    }
}/// <summary>
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


using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml;
using SpreadsheetUtilities;

namespace SS;

/// <summary>
///     Utility class for keeping regex stuff since it needs to be a partial
///     in order to be a comp time regex imp
/// </summary>
internal static partial class Utility
{
    private static readonly Regex ValidName = _validName();
    private static readonly Regex Nothing = _nothing();

    [GeneratedRegex(@"^\w[\w\d]*$", RegexOptions.IgnorePatternWhitespace |
                                    RegexOptions.NonBacktracking)]
    private static partial Regex _validName();

    /// <summary>
    ///     checks if a cell name is invalid
    /// </summary>
    /// <param name="s">the cell name to check</param>
    /// <returns></returns>
    public static bool IsInvalidName(string s)
    {
        return !ValidName.IsMatch(s);
    }


    [GeneratedRegex(@"^$", RegexOptions.IgnorePatternWhitespace |
                           RegexOptions.NonBacktracking)]
    private static partial Regex _nothing();

    /// <summary>
    ///     checks if a string is empty
    /// </summary>
    /// <param name="s">the string that could be empty</param>
    /// <returns></returns>
    public static bool IsNothing(string s)
    {
        return Nothing.IsMatch(s);
    }
}

/// <summary>
///     my implementation of the abstract spreadsheet
/// </summary>
public class Spreadsheet : AbstractSpreadsheet
{
    //graph and its utility function
    private readonly DependencyGraph _graph = new();

    //the cells with cell objects inside
    protected readonly Dictionary<string, ICell> cells = [];

    // whether the spreadsheet has changed

    /// <summary>
    ///     constructor for a new unsaved spreadsheet
    /// </summary>
    /// <param name="isValid">determines if a var is valid</param>
    /// <param name="normalize">normalizes the cell names</param>
    /// <param name="version">the version of this spreadsheet</param>
    public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor with no parameters that puts default values in
    /// </summary>
    public Spreadsheet() : base(_ => true, s => s, "1")
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor that reads from a file and stores it in this spreadsheet
    /// </summary>
    /// <param name="path">path to the file</param>
    /// <param name="isValid">checks if var is valid</param>
    /// <param name="normalize">normalize name of var</param>
    /// <param name="version">the version string</param>
    public Spreadsheet(string path, Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
        try
        {
            using var reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (!(reader.IsStartElement() && reader.Name == "cell")) continue;
                var name = "";
                var content = "";
                while (reader.Read())
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.EndElement:
                            if (reader.Name == "cell")
                                goto
                                    exitLoop; //AAH CALL THE POLICE ITS A GOTO (no break loop in switch and no labeled break)
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

                exitLoop:
                if (!name.Equals("") && !content.Equals("")) SetContentsOfCell(name, content);
            }
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }
    }

    public override bool Changed { get; protected set; }

    /// <summary>
    ///     utility function for the graph that's not super useful
    /// </summary>
    /// <param name="name">the cell to get dependents of</param>
    /// <returns></returns>
    protected override IEnumerable<string> GetDirectDependents(string name)
    {
        return _graph.GetDependees(name);
    }


    /// <summary>
    ///     a function to write xml to and xml writer
    /// </summary>
    /// <param name="xmlWriter">the writer to write this sheet to</param>
    private void DoXmlWriting(XmlWriter xmlWriter)
    {
        xmlWriter.WriteStartDocument(); //<xml ...
        xmlWriter.WriteStartElement("spreadsheet"); //<spreadsheet>
        xmlWriter.WriteAttributeString("version", Version);
        foreach (var (name, cell) in cells)
        {
            xmlWriter.WriteStartElement("cell"); //<cell>
            xmlWriter.WriteStartElement("name"); //<name>
            xmlWriter.WriteValue(name); //[name]
            xmlWriter.WriteEndElement(); //</name>
            xmlWriter.WriteStartElement("contents"); //contents>
            xmlWriter.WriteValue(cell.Contents(true)); //[contents]
            xmlWriter.WriteEndElement(); //</contents>
            xmlWriter.WriteEndElement(); //</cell>
        }

        xmlWriter.WriteEndElement(); //</spreadsheet>
        xmlWriter.WriteEndDocument();
    }


    /// <summary>
    ///     virtual stack implementation of the base.GetCellsToRecalculate
    ///     ~23% better performance but also matches the (undefined behavior based)
    ///     testStress4 problematic types of test (ISet).SequenceEquals(IEnumerable)
    ///     lemme go check how much I'm paying for a CS education where they
    ///     dont do code reviews on the course materials
    /// </summary>
    /// <param name="start">start cell</param>
    /// <returns></returns>
    private new RecalculatedCellList GetCellsToRecalculate(string start)
    {
        return new RecalculatedCellList(start, this);
    }

    /// <summary>
    ///     set a cell to a string value
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="text">the string content</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, string text)
    {
        //should be empty but whatever
        IList<string> deps = GetCellsToRecalculate(name);

        cells.Remove(name);
        //store only if its not empty
        ICell newCell = new StringCell(text);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     set a cell to a double
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="number">the double value to store</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, double number)
    {
        IList<string> deps = GetCellsToRecalculate(name);

        //if its a new cell do recalculations
        ICell newCell = new DoubleCell(number);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    protected override IList<string> SetCellContents(string name, Formula formula)
    {
        //check if depends on its own vars
        if (formula.GetVariables().Contains(name)) throw new CircularException();
        //get recursive deps
        IList<string> deps = GetCellsToRecalculate(name);
        //check if the recursive deps include its own deps
        if (deps.Intersect(formula.GetVariables()).Any()) throw new CircularException();

        //if its a new cell do recalculations
        ICell newCell = new FormulaCell(formula, this);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, formula.GetVariables());
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     get all the cells you've populated
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override IEnumerable<string> GetNamesOfAllNonemptyCells()
    {
        return cells.Keys;
        //.Select(UnPreHash);
    }

    /// <summary>
    ///     get the string version from the xml file with the given path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename">the file to check</param>
    /// <returns>the string it found</returns>
    /// <exception cref="SpreadsheetReadWriteException">if something goes wrong with reading</exception>
    public override string GetSavedVersion(string filename)
    {
        try
        {
            string? maybeVersion;
            using var reader = XmlReader.Create(filename);
            while (reader.Read())
                if (reader.IsStartElement() && reader.Name == "spreadsheet" &&
                    (maybeVersion = reader.GetAttribute("version"))?.GetType() == typeof(string))
                    return maybeVersion;
                else throw new SpreadsheetReadWriteException("Read failed");
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }

        throw new SpreadsheetReadWriteException("Read failed");
    }

    /// <summary>
    ///     saves a file to a path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename"></param>
    /// <exception cref="SpreadsheetReadWriteException">if the file cant be saved for some reason</exception>
    public override void Save(string filename)
    {
        Changed = false;
        try
        {
            using var xmlWriter =
                XmlWriter.Create(filename, new XmlWriterSettings { Indent = true, IndentChars = "  " });
            DoXmlWriting(xmlWriter);
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Save failed");
        }
    }

    /// <summary>
    ///     gets xml string version of this spreadsheet
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override string GetXML()
    {
        StringWriter stringWriter = new();
        DoXmlWriting(
            XmlWriter.Create(
                stringWriter,
                new XmlWriterSettings { Indent = true, IndentChars = "  " }));
        return stringWriter.ToString();
    }


    /// <summary>
    ///     tries to get a cell value
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a value from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellValue(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var cell) ? cell.Value : "";
    }

    /// <summary>
    ///     tries to get a cell contents
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a contents from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellContents(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(false) : "";
    }

    public object GetCellContents(string name, bool better)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(better) : "";
    }

    /// <summary>
    ///     set the content of the cell and return recursive deps
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">name of cell to set</param>
    /// <param name="content">content to set the cell to</param>
    /// <returns>the dependees of this cell</returns>
    /// <exception cref="InvalidNameException">if the cell name is not a good name</exception>
    public override IList<string> SetContentsOfCell(string name, string content)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        Changed = true;
        return double.TryParse(content, out var d) ? SetCellContents(name, d) :
            content.StartsWith('=') ? SetCellContents(name, new Formula(content[1..], Normalize, IsValid)) :
            SetCellContents(name, content);
    }

    public string Compile(string name)
    {
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name + "Assembly"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(name + "Module");
        var typeBuilder =
            moduleBuilder.DefineType("sheetSpace.sheetLibrary", TypeAttributes.Public | TypeAttributes.Class);
        var fields = new Dictionary<string, FieldBuilder>(); //field references to create setters/getters
        var computeFunctions = new Dictionary<string, MethodBuilder>(); //field references to create setters

        //define all fields and getters
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName].Value is not double) continue;
            var fb = typeBuilder.DefineField("_" + cellName, typeof(double), FieldAttributes.Private);
            fields[cellName] = fb;
            var getterMethod = typeBuilder.DefineMethod("Get_" + cellName, MethodAttributes.Public, typeof(double),
                Type.EmptyTypes);
            var getterIl = getterMethod.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fields[cellName]);
            getterIl.Emit(OpCodes.Ret);
        }

        //define constructor
        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);
        var ctorIl = ctor.GetILGenerator();
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName].Value is not double d) continue;
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldc_R8, d);
            ctorIl.Emit(OpCodes.Stfld, fields[cellName]);
        }

        ctorIl.Emit(OpCodes.Ret);

        //define all compute formula functions
        foreach (var cellName in cells.Keys)
        {
            if (cells[cellName] is StringCell || cells[cellName] is DoubleCell ||
                cells[cellName].Value is not double) continue;
            var computeMethod = typeBuilder.DefineMethod("Compute_" + cellName, MethodAttributes.Private, typeof(void),
                Type.EmptyTypes);
            var computeIl = computeMethod.GetILGenerator();
            computeIl.Emit(OpCodes.Ldarg_0);
            ((FormulaCell)cells[cellName]).Compile(computeIl, fields);
            computeIl.Emit(OpCodes.Stfld, fields[cellName]);
            computeIl.Emit(OpCodes.Ret);
            computeFunctions[cellName] = computeMethod;
        }

        //define all field setters to compute relevant fields
        foreach (var cellName in cells.Keys)
        {
            IEnumerable<string> deps = GetCellsToRecalculate(cellName);
            if (cells[cellName] is StringCell || cells[cellName] is FormulaCell ||
                cells[cellName].Value is not double) continue;
            var setterMethod = typeBuilder.DefineMethod("Put_" + cellName, MethodAttributes.Public, typeof(void),
                [typeof(double)]);
            var setCellMsilGenerator = setterMethod.GetILGenerator();
            setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
            setCellMsilGenerator.Emit(OpCodes.Ldarg_1);
            setCellMsilGenerator.Emit(OpCodes.Stfld, fields[cellName]);

            // Dependency recalculation:
            foreach (var depCell in deps)
            {
                if (!computeFunctions.TryGetValue(depCell, out var b)) continue;
                setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
                setCellMsilGenerator.Emit(OpCodes.Callvirt, b);
            }

            setCellMsilGenerator.Emit(OpCodes.Ret);
        }

        typeBuilder.CreateType();
        var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".dll");
        File.Delete(fullPath);
        //assemblyBuilder.Save(fullPath);
        return fullPath;
    }

    /// <summary>
    ///     cell interface for cell structs
    /// </summary>
    protected interface ICell : IEquatable<ICell>
    {
        public object Value { get; }
        public object CompItem { get; }
        public object Contents(bool forSave);
        public void Compute();
    }

    /// <summary>
    ///     doesnt do much just stores a double and returns it
    /// </summary>
    /// <param name="d">the double to store</param>
    private readonly struct DoubleCell(double d) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly double _value = d;
    }

    /// <summary>
    ///     doesnt do much just stores a string and returns it
    /// </summary>
    /// <param name="s">the string to store</param>
    private readonly struct StringCell(string s) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly string _value = s;
    }

    /// <summary>
    ///     a cell to store formulas
    /// </summary>
    /// <param name="f">the formula</param>
    /// <param name="s">a reference to the spreadsheet to access other cells</param>
    private struct FormulaCell(Formula f, Spreadsheet s) : ICell
    {
        public readonly object Contents(bool forSave)
        {
            return (forSave ? "=" : "") + _content;
        }

        public object Value { get; private set; } = new FormulaError("Never Calculated Struct");

        public void Compute()
        {
            var vars = _content.GetVariables();
            foreach (var dep in vars)
                if (!_spreadsheet.cells.TryGetValue(dep, out var cell) || cell.Value.GetType() != typeof(double))
                {
                    Value = new FormulaError("Dependency Not Valid");
                    return;
                }

            try
            {
                Value = _content.Evaluate(Lookup);
            }
            catch (Exception e)
            {
                Value = new FormulaError("Dependency Not Valid " + e.Message);
            }
        }

        public readonly object CompItem => _content;

        public readonly bool Equals(ICell? other)
        {
            return other is FormulaCell && other.CompItem == CompItem;
        }

        private readonly Formula _content = f;
        private readonly Spreadsheet _spreadsheet = s;

        private readonly double Lookup(string s)
        {
            var cell = _spreadsheet.cells[s];
            var val = (double)cell.Value;
            return val;
        }

        public void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields)
        {
            _content.Compile(gen, fields);
        }
    }

    /// <summary>
    ///     A dep list that only does stuff if its ever used
    /// </summary>
    /// <param name="name">cell name</param>
    /// <param name="s">the spreadsheet reference</param>
    private struct RecalculatedCellList(string name, Spreadsheet s) : IList<string>
    {
        //emulate recursive stack frame
        //first half is a proxy for the return pointer
        //(which is either at the start of the function of halfway through)
        //name is a string stack variable
        private struct RecomputeStackFrame
        {
            public string name;
            public bool firstHalf;
        }

        private static readonly Stack<RecomputeStackFrame> VirtualCallStack = new();
        private IList<string>? _actualList;
        private readonly Spreadsheet _spreadsheet = s;
        private readonly string _start = name;

        private IList<string> EnsureList()
        {
            return _actualList ??= GetList();
        }

        private IList<string> GetList()
        {
            HashSet<string> visited = [];
            //obligatory "linked list bad" comment (because linked lists are BAD!)
            //linked list alone causes about a 3rd of the slowdown from the
            //true recursion implementation
            Stack<string> changed = new();
            // c# does not support tail call optimizations

            //same as original
            //"Call" Visit(start...)
            RecomputeStackFrame frame = new() { name = _start, firstHalf = true };
            lock (VirtualCallStack)
            {
                do
                {
                    if (frame.firstHalf) //ret pops either &Visit or &Visit + K
                    {
                        visited.Add(frame.name);
                        frame.firstHalf = false;
                        VirtualCallStack.Push(frame);
                        foreach (var dep in _spreadsheet.GetDirectDependents(frame.name))
                            if (!visited.Contains(dep))
                                VirtualCallStack.Push(new RecomputeStackFrame { name = dep, firstHalf = true });
                    }
                    else
                    {
                        changed.Push(frame.name);
                    }
                } while (VirtualCallStack.TryPop(out frame));
            }

            return [.. changed];
        }

        public string this[int index]
        {
            get => EnsureList()[index];
            set => EnsureList()[index] = value;
        }

        public int Count => EnsureList().Count;
        public readonly bool IsReadOnly => true;

        public void Add(string item)
        {
            EnsureList().Add(item);
        }

        public void Clear()
        {
            EnsureList().Clear();
        }

        public bool Contains(string item)
        {
            return EnsureList().Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            EnsureList().CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return EnsureList().GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return EnsureList().IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            EnsureList().Insert(index, item);
        }

        public bool Remove(string item)
        {
            return EnsureList().Remove(item);
        }

        public void RemoveAt(int index)
        {
            EnsureList().RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)EnsureList()).GetEnumerator();
        }
    }
}/// <summary>
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


using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml;
using SpreadsheetUtilities;

namespace SS;

/// <summary>
///     Utility class for keeping regex stuff since it needs to be a partial
///     in order to be a comp time regex imp
/// </summary>
internal static partial class Utility
{
    private static readonly Regex ValidName = _validName();
    private static readonly Regex Nothing = _nothing();

    [GeneratedRegex(@"^\w[\w\d]*$", RegexOptions.IgnorePatternWhitespace |
                                    RegexOptions.NonBacktracking)]
    private static partial Regex _validName();

    /// <summary>
    ///     checks if a cell name is invalid
    /// </summary>
    /// <param name="s">the cell name to check</param>
    /// <returns></returns>
    public static bool IsInvalidName(string s)
    {
        return !ValidName.IsMatch(s);
    }


    [GeneratedRegex(@"^$", RegexOptions.IgnorePatternWhitespace |
                           RegexOptions.NonBacktracking)]
    private static partial Regex _nothing();

    /// <summary>
    ///     checks if a string is empty
    /// </summary>
    /// <param name="s">the string that could be empty</param>
    /// <returns></returns>
    public static bool IsNothing(string s)
    {
        return Nothing.IsMatch(s);
    }
}

/// <summary>
///     my implementation of the abstract spreadsheet
/// </summary>
public class Spreadsheet : AbstractSpreadsheet
{
    //graph and its utility function
    private readonly DependencyGraph _graph = new();

    //the cells with cell objects inside
    protected readonly Dictionary<string, ICell> cells = [];

    // whether the spreadsheet has changed

    /// <summary>
    ///     constructor for a new unsaved spreadsheet
    /// </summary>
    /// <param name="isValid">determines if a var is valid</param>
    /// <param name="normalize">normalizes the cell names</param>
    /// <param name="version">the version of this spreadsheet</param>
    public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor with no parameters that puts default values in
    /// </summary>
    public Spreadsheet() : base(_ => true, s => s, "1")
    {
        Changed = false;
    }

    /// <summary>
    ///     a constructor that reads from a file and stores it in this spreadsheet
    /// </summary>
    /// <param name="path">path to the file</param>
    /// <param name="isValid">checks if var is valid</param>
    /// <param name="normalize">normalize name of var</param>
    /// <param name="version">the version string</param>
    public Spreadsheet(string path, Func<string, bool> isValid, Func<string, string> normalize, string version)
        : base(isValid, normalize, version)
    {
        Changed = false;
        try
        {
            using var reader = XmlReader.Create(path);
            while (reader.Read())
            {
                if (!(reader.IsStartElement() && reader.Name == "cell")) continue;
                var name = "";
                var content = "";
                while (reader.Read())
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.EndElement:
                            if (reader.Name == "cell")
                                goto
                                    exitLoop; //AAH CALL THE POLICE ITS A GOTO (no break loop in switch and no labeled break)
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

                exitLoop:
                if (!name.Equals("") && !content.Equals("")) SetContentsOfCell(name, content);
            }
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }
    }

    public override bool Changed { get; protected set; }

    /// <summary>
    ///     utility function for the graph that's not super useful
    /// </summary>
    /// <param name="name">the cell to get dependents of</param>
    /// <returns></returns>
    protected override IEnumerable<string> GetDirectDependents(string name)
    {
        return _graph.GetDependees(name);
    }


    /// <summary>
    ///     a function to write xml to and xml writer
    /// </summary>
    /// <param name="xmlWriter">the writer to write this sheet to</param>
    private void DoXmlWriting(XmlWriter xmlWriter)
    {
        xmlWriter.WriteStartDocument(); //<xml ...
        xmlWriter.WriteStartElement("spreadsheet"); //<spreadsheet>
        xmlWriter.WriteAttributeString("version", Version);
        foreach (var (name, cell) in cells)
        {
            xmlWriter.WriteStartElement("cell"); //<cell>
            xmlWriter.WriteStartElement("name"); //<name>
            xmlWriter.WriteValue(name); //[name]
            xmlWriter.WriteEndElement(); //</name>
            xmlWriter.WriteStartElement("contents"); //contents>
            xmlWriter.WriteValue(cell.Contents(true)); //[contents]
            xmlWriter.WriteEndElement(); //</contents>
            xmlWriter.WriteEndElement(); //</cell>
        }

        xmlWriter.WriteEndElement(); //</spreadsheet>
        xmlWriter.WriteEndDocument();
    }


    /// <summary>
    ///     virtual stack implementation of the base.GetCellsToRecalculate
    ///     ~23% better performance but also matches the (undefined behavior based)
    ///     testStress4 problematic types of test (ISet).SequenceEquals(IEnumerable)
    ///     lemme go check how much I'm paying for a CS education where they
    ///     dont do code reviews on the course materials
    /// </summary>
    /// <param name="start">start cell</param>
    /// <returns></returns>
    private new RecalculatedCellList GetCellsToRecalculate(string start)
    {
        return new RecalculatedCellList(start, this);
    }

    /// <summary>
    ///     set a cell to a string value
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="text">the string content</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, string text)
    {
        //should be empty but whatever
        IList<string> deps = GetCellsToRecalculate(name);

        cells.Remove(name);
        //store only if its not empty
        ICell newCell = new StringCell(text);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     set a cell to a double
    /// </summary>
    /// <param name="name">the name of the cell (assumed to be valid)</param>
    /// <param name="number">the double value to store</param>
    /// <returns></returns>
    protected override IList<string> SetCellContents(string name, double number)
    {
        IList<string> deps = GetCellsToRecalculate(name);

        //if its a new cell do recalculations
        ICell newCell = new DoubleCell(number);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, []);
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    protected override IList<string> SetCellContents(string name, Formula formula)
    {
        //check if depends on its own vars
        if (formula.GetVariables().Contains(name)) throw new CircularException();
        //get recursive deps
        IList<string> deps = GetCellsToRecalculate(name);
        //check if the recursive deps include its own deps
        if (deps.Intersect(formula.GetVariables()).Any()) throw new CircularException();

        //if its a new cell do recalculations
        ICell newCell = new FormulaCell(formula, this);
        if (cells.TryGetValue(name, out var oldCell) && newCell == oldCell) return deps;
        cells[name] = newCell;
        _graph.ReplaceDependents(name, formula.GetVariables());
        newCell.Compute();
        foreach (var n in deps)
            if (cells.TryGetValue(n, out var cell))
                cell.Compute();
        return deps;
    }

    /// <summary>
    ///     get all the cells you've populated
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override IEnumerable<string> GetNamesOfAllNonemptyCells()
    {
        return cells.Keys;
        //.Select(UnPreHash);
    }

    /// <summary>
    ///     get the string version from the xml file with the given path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename">the file to check</param>
    /// <returns>the string it found</returns>
    /// <exception cref="SpreadsheetReadWriteException">if something goes wrong with reading</exception>
    public override string GetSavedVersion(string filename)
    {
        try
        {
            string? maybeVersion;
            using var reader = XmlReader.Create(filename);
            while (reader.Read())
                if (reader.IsStartElement() && reader.Name == "spreadsheet" &&
                    (maybeVersion = reader.GetAttribute("version"))?.GetType() == typeof(string))
                    return maybeVersion;
                else throw new SpreadsheetReadWriteException("Read failed");
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Read failed");
        }

        throw new SpreadsheetReadWriteException("Read failed");
    }

    /// <summary>
    ///     saves a file to a path
    /// </summary>
    /// <inheritdoc />
    /// <param name="filename"></param>
    /// <exception cref="SpreadsheetReadWriteException">if the file cant be saved for some reason</exception>
    public override void Save(string filename)
    {
        Changed = false;
        try
        {
            using var xmlWriter =
                XmlWriter.Create(filename, new XmlWriterSettings { Indent = true, IndentChars = "  " });
            DoXmlWriting(xmlWriter);
        }
        catch
        {
            throw new SpreadsheetReadWriteException("Save failed");
        }
    }

    /// <summary>
    ///     gets xml string version of this spreadsheet
    /// </summary>
    /// <inheritdoc />
    /// <returns></returns>
    public override string GetXML()
    {
        StringWriter stringWriter = new();
        DoXmlWriting(
            XmlWriter.Create(
                stringWriter,
                new XmlWriterSettings { Indent = true, IndentChars = "  " }));
        return stringWriter.ToString();
    }


    /// <summary>
    ///     tries to get a cell value
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a value from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellValue(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var cell) ? cell.Value : "";
    }

    /// <summary>
    ///     tries to get a cell contents
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">the cell name to try to get a contents from</param>
    /// <returns></returns>
    /// <exception cref="InvalidNameException">if the name is a bad name</exception>
    public override object GetCellContents(string name)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(false) : "";
    }

    public object GetCellContents(string name, bool better)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        return cells.TryGetValue(name, out var value) ? value.Contents(better) : "";
    }

    /// <summary>
    ///     set the content of the cell and return recursive deps
    /// </summary>
    /// <inheritdoc />
    /// <param name="name">name of cell to set</param>
    /// <param name="content">content to set the cell to</param>
    /// <returns>the dependees of this cell</returns>
    /// <exception cref="InvalidNameException">if the cell name is not a good name</exception>
    public override IList<string> SetContentsOfCell(string name, string content)
    {
        if (Utility.IsInvalidName(name)) throw new InvalidNameException();
        Changed = true;
        return double.TryParse(content, out var d) ? SetCellContents(name, d) :
            content.StartsWith('=') ? SetCellContents(name, new Formula(content[1..], Normalize, IsValid)) :
            SetCellContents(name, content);
    }

    public string Compile(string name)
        {
            var assemblyBuilder = AssemblyBuilder.DefinePersistedAssembly(new AssemblyName(name + "Assembly"), typeof(object).Assembly, new CustomAttributeBuilder[0]);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(name + "Module");
            var typeBuilder = moduleBuilder.DefineType("sheetSpace.sheetLibrary", TypeAttributes.Public | TypeAttributes.Class);
            var fields = new Dictionary<string, FieldBuilder>(); //field references to create setters/getters
            var computeFunctions = new Dictionary<string, MethodBuilder>(); //field references to create setters
            
            //define all fields and getters
            foreach (var cellName in cells.Keys)
            {
                if (cells[cellName] is StringCell || cells[cellName].Value is not double) continue;
                var fb = typeBuilder.DefineField("_" + cellName, typeof(double), FieldAttributes.Private);
                fields[cellName] = fb;
                var getterMethod = typeBuilder.DefineMethod("Get_" + cellName, MethodAttributes.Public , typeof(double), Type.EmptyTypes);
                var getterIl = getterMethod.GetILGenerator();
                getterIl.Emit(OpCodes.Ldarg_0);
                getterIl.Emit(OpCodes.Ldfld, fields[cellName]);
                getterIl.Emit(OpCodes.Ret);
            }

            //define constructor
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, []);
            var ctorIl = ctor.GetILGenerator();
            foreach (var cellName in cells.Keys)
            {
                if (cells[cellName] is StringCell || cells[cellName].Value is not double d) continue;
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Ldc_R8, d);
                ctorIl.Emit(OpCodes.Stfld, fields[cellName]);
            }
            ctorIl.Emit(OpCodes.Ret);

            //define all compute formula functions
            foreach (var cellName in cells.Keys)
            {
                if (cells[cellName] is StringCell || cells[cellName] is DoubleCell || cells[cellName].Value is not double) continue;
                var computeMethod = typeBuilder.DefineMethod("Compute_" + cellName, MethodAttributes.Private, typeof(void), Type.EmptyTypes);
                var computeIl = computeMethod.GetILGenerator();
                computeIl.Emit(OpCodes.Ldarg_0);
                ((FormulaCell)cells[cellName]).Compile(computeIl, fields);
                computeIl.Emit(OpCodes.Stfld, fields[cellName]);
                computeIl.Emit(OpCodes.Ret);
                computeFunctions[cellName] = computeMethod;
            }
            
            //define all field setters to compute relevant fields
            foreach (var cellName in cells.Keys)
            {
                IEnumerable<string> deps = GetCellsToRecalculate(cellName);
                if (cells[cellName] is StringCell || cells[cellName] is FormulaCell || cells[cellName].Value is not double) continue;
                var setterMethod = typeBuilder.DefineMethod("Put_" + cellName, MethodAttributes.Public, typeof(void), [typeof(double)]);
                var setCellMsilGenerator = setterMethod.GetILGenerator();
                setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
                setCellMsilGenerator.Emit(OpCodes.Ldarg_1);
                setCellMsilGenerator.Emit(OpCodes.Stfld, fields[cellName]);
                
                // Dependency recalculation:
                foreach (var depCell in deps)
                {
                    if (!computeFunctions.TryGetValue(depCell, out var b)) continue;
                    setCellMsilGenerator.Emit(OpCodes.Ldarg_0);
                    setCellMsilGenerator.Emit(OpCodes.Callvirt, b);
                }
                setCellMsilGenerator.Emit(OpCodes.Ret);
            }
            typeBuilder.CreateType();
            var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".dll");
            System.IO.File.Delete(fullPath);
            assemblyBuilder.Save(fullPath);
            return fullPath;
        }

    /// <summary>
    ///     cell interface for cell structs
    /// </summary>
    protected interface ICell : IEquatable<ICell>
    {
        public object Value { get; }
        public object CompItem { get; }
        public object Contents(bool forSave);
        public void Compute();
    }

    /// <summary>
    ///     doesnt do much just stores a double and returns it
    /// </summary>
    /// <param name="d">the double to store</param>
    private readonly struct DoubleCell(double d) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly double _value = d;
    }

    /// <summary>
    ///     doesnt do much just stores a string and returns it
    /// </summary>
    /// <param name="s">the string to store</param>
    private readonly struct StringCell(string s) : ICell
    {
        public readonly object Value => _value;

        public readonly object Contents(bool forSave)
        {
            return _value;
        }

        public void Compute()
        {
        }

        public object CompItem => Value;

        public bool Equals(ICell? other)
        {
            return other?.GetType() == GetType() && other.CompItem == CompItem;
        }

        private readonly string _value = s;
    }

    /// <summary>
    ///     a cell to store formulas
    /// </summary>
    /// <param name="f">the formula</param>
    /// <param name="s">a reference to the spreadsheet to access other cells</param>
    private struct FormulaCell(Formula f, Spreadsheet s) : ICell
    {
        public readonly object Contents(bool forSave)
        {
            return (forSave ? "=" : "") + _content;
        }

        public object Value { get; private set; } = new FormulaError("Never Calculated Struct");

        public void Compute()
        {
            var vars = _content.GetVariables();
            foreach (var dep in vars)
                if (!_spreadsheet.cells.TryGetValue(dep, out var cell) || cell.Value.GetType() != typeof(double))
                {
                    Value = new FormulaError("Dependency Not Valid");
                    return;
                }

            try
            {
                Value = _content.Evaluate(Lookup);
            }
            catch (Exception e)
            {
                Value = new FormulaError("Dependency Not Valid " + e.Message);
            }
        }

        public readonly object CompItem => _content;

        public readonly bool Equals(ICell? other)
        {
            return other is FormulaCell && other.CompItem == CompItem;
        }

        private readonly Formula _content = f;
        private readonly Spreadsheet _spreadsheet = s;

        private readonly double Lookup(string s)
        {
            var cell = _spreadsheet.cells[s];
            var val = (double)cell.Value;
            return val;
        }

        public void Compile(ILGenerator gen, Dictionary<string, FieldBuilder> fields)
        {
            _content.Compile(gen, fields);
        }
    }

    /// <summary>
    ///     A dep list that only does stuff if its ever used
    /// </summary>
    /// <param name="name">cell name</param>
    /// <param name="s">the spreadsheet reference</param>
    private struct RecalculatedCellList(string name, Spreadsheet s) : IList<string>
    {
        //emulate recursive stack frame
        //first half is a proxy for the return pointer
        //(which is either at the start of the function of halfway through)
        //name is a string stack variable
        private struct RecomputeStackFrame
        {
            public string name;
            public bool firstHalf;
        }

        private static readonly Stack<RecomputeStackFrame> VirtualCallStack = new();
        private IList<string>? _actualList;
        private readonly Spreadsheet _spreadsheet = s;
        private readonly string _start = name;

        private IList<string> EnsureList()
        {
            return _actualList ??= GetList();
        }

        private IList<string> GetList()
        {
            HashSet<string> visited = [];
            //obligatory "linked list bad" comment (because linked lists are BAD!)
            //linked list alone causes about a 3rd of the slowdown from the
            //true recursion implementation
            Stack<string> changed = new();
            // c# does not support tail call optimizations

            //same as original
            //"Call" Visit(start...)
            RecomputeStackFrame frame = new() { name = _start, firstHalf = true };
            lock (VirtualCallStack)
            {
                do
                {
                    if (frame.firstHalf) //ret pops either &Visit or &Visit + K
                    {
                        visited.Add(frame.name);
                        frame.firstHalf = false;
                        VirtualCallStack.Push(frame);
                        foreach (var dep in _spreadsheet.GetDirectDependents(frame.name))
                            if (!visited.Contains(dep))
                                VirtualCallStack.Push(new RecomputeStackFrame { name = dep, firstHalf = true });
                    }
                    else
                    {
                        changed.Push(frame.name);
                    }
                } while (VirtualCallStack.TryPop(out frame));
            }

            return [.. changed];
        }

        public string this[int index]
        {
            get => EnsureList()[index];
            set => EnsureList()[index] = value;
        }

        public int Count => EnsureList().Count;
        public readonly bool IsReadOnly => true;

        public void Add(string item)
        {
            EnsureList().Add(item);
        }

        public void Clear()
        {
            EnsureList().Clear();
        }

        public bool Contains(string item)
        {
            return EnsureList().Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            EnsureList().CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return EnsureList().GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return EnsureList().IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            EnsureList().Insert(index, item);
        }

        public bool Remove(string item)
        {
            return EnsureList().Remove(item);
        }

        public void RemoveAt(int index)
        {
            EnsureList().RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)EnsureList()).GetEnumerator();
        }
    }
}