using SpreadsheetUtilities;
using SS;

namespace GUI;

public partial class MainPage : ContentPage
{
    private const int Rows = 200;
    private const int Columns = 26;
    private const int Widths = 200;
    private const int Heights = 35;
    private const int StrokeSize = 1;
    private static readonly Color BgColor = Colors.LightGrey;

    private static readonly Color Bg2Color = Colors.LightGrey;

    private static readonly Color Bg3Color = Colors.Gray;

    private readonly Grid _grid;


    private readonly Dictionary<string, Border> _labels = [];

    private Action _destroyCurrent = () => { };
    private readonly Border _hover = new() { Stroke = Colors.Coral };


    private Point _lastGridPoint = new(0, 0);
    private readonly Border _selection = new() { Stroke = Colors.Chartreuse };
    private Spreadsheet _spreadsheet;
    private string FilenameAfterSave;

    public MainPage() //might need to change how this is loaded
    {
        //Keep at top
        InitializeComponent();

        //make new spreadsheet to model
        _spreadsheet = new Spreadsheet(SS.Utility.IsValidVar, s => s.ToUpper(), "six");

        //definitions for grid
        var rowsArray = new RowDefinition[Rows];
        var colsArray = new ColumnDefinition[Columns];
        for (var i = 0; i < Rows; i++) rowsArray[i] = new RowDefinition(Heights);
        for (var i = 0; i < Columns; i++) colsArray[i] = new ColumnDefinition(Widths);
        var rowsDef = new RowDefinitionCollection(rowsArray);
        var colsDef = new ColumnDefinitionCollection(colsArray);

        //Create grid
        _grid = new Grid
        {
            RowDefinitions = rowsDef,
            ColumnDefinitions = colsDef,
            WidthRequest = Columns * Widths,
            HeightRequest = Rows * Heights
        };
        //Insert Grid
        Grid.Content = _grid;

        SetupGridListenersForCellSelection();
        MakeLabels(rowsDef, colsArray, colsDef, rowsArray);
        BindLabelScrollingToGRidScrolling();
        CreateHoverBars();
        BindSizeChanges();
    }

    private void SetupGridListenersForCellSelection()
    {
        //Add Tap listener for editing cell
        var taps = new TapGestureRecognizer();
        taps.Tapped += (_, e) => OnGridTapped((Point)e.GetPosition(_grid));
        _grid.GestureRecognizers.Add(taps);
        //SetDefaultCell
        OnGridTapped(_lastGridPoint);
        CellInfoContent.Focused += (_, _) => OnGridTapped(_lastGridPoint);
    }

    private void BindSizeChanges()
    {
        SizeChanged += (sender, args) =>
        {
            var width = Width;
            var height = Height;
            Entire.WidthRequest = width;
            Entire.HeightRequest = height - Heights;
            Border.WidthRequest = width - StrokeSize;
            TopLabelsHolder.WidthRequest = width - StrokeSize;
            TopLabels.WidthRequest = width - Widths - StrokeSize;
            Table.WidthRequest = width - StrokeSize;
            Table.HeightRequest = Entire.HeightRequest - Heights - 6;
            Grid.HeightRequest = Math.Min(Table.HeightRequest - Heights, Heights * Rows);
            Grid.WidthRequest = width - Widths - StrokeSize;
            LeftLabels.HeightRequest = Grid.HeightRequest;
        };
    }

    private void CreateHoverBars()
    {
        //Create hover guide bars
        var rowBar = new Label();
        var colBar = new Label();
        var hover = new PointerGestureRecognizer();
        hover.PointerMoved += (sender, e) =>
        {
            var pos = (Point)e.GetPosition(_grid);
            var entryI = (int)(pos.X / Widths);
            var entryJ = (int)(pos.Y / Heights);
            var cellName = GetCellName(entryI, entryJ);
            _grid.Remove(_hover);
            _grid.Add(_hover, entryI, entryJ);

            _grid.Remove(rowBar);
            _grid.Remove(colBar);
            rowBar = new Label
            {
                BackgroundColor = new Color(0, 0, 0, 10),
                ZIndex = 2,
                WidthRequest = _grid.Width * 4
            };
            colBar = new Label
            {
                BackgroundColor = new Color(0, 0, 0, 10),
                ZIndex = 2,
                HeightRequest = _grid.Height * 4
            };

            HoverCell.Text = "Hover: " + cellName;
            _grid.Add(rowBar, 0, Rows + 1, entryJ, entryJ + 1);
            _grid.Add(colBar, entryI, entryI + 1, 0, Columns + 1);
        };
        hover.PointerExited += (_, _) =>
        {
            _grid.Remove(rowBar);
            _grid.Remove(colBar);
            HoverCell.Text = "Hover: " + "--";
        };
        _grid.GestureRecognizers.Add(hover);
    }

    private void BindLabelScrollingToGRidScrolling()
    {
        //Bind Scrolling Together
        Grid.Scrolled += (_, e) => TopLabels.ScrollToAsync(e.ScrollX, TopLabels.ScrollY, false);
        TopLabels.Scrolled += (_, e) => Grid.ScrollToAsync(e.ScrollX, Grid.ScrollY, false);
        Grid.Scrolled += (_, e) => LeftLabels.ScrollToAsync(LeftLabels.ScrollX, e.ScrollY, false);
        LeftLabels.Scrolled += (_, e) => Grid.ScrollToAsync(Grid.ScrollX, e.ScrollY, false);
    }

    private void MakeLabels(RowDefinitionCollection rowsDef, ColumnDefinition[] colsArray,
        ColumnDefinitionCollection colsDef, RowDefinition[] rowsArray)
    {
        //Make labels
        nullCell.WidthRequest = Widths;
        var leftLabels = new Grid
        {
            RowDefinitions = rowsDef,
            WidthRequest = Widths,
            HeightRequest = Rows * Heights,
            ColumnDefinitions = { colsArray[0] }
        };
        var columnLabels = new Grid
        {
            ColumnDefinitions = colsDef,
            HeightRequest = Heights,
            WidthRequest = Columns * Widths,
            RowDefinitions = { rowsArray[0] }
        };

        //Populate labels
        for (var i = 0; i < Rows; i++) AddEntry(leftLabels, "" + i, i, 0);
        for (var i = 0; i < Columns; i++) AddEntry(columnLabels, "" + (char)('A' + i), 0, i);

        //Insert labels
        TopLabels.Content = columnLabels;
        LeftLabels.Content = leftLabels;
    }


    private static void AddEntry(Grid layoutToAddTo, string entryText, int row,
        int col)
    {
        layoutToAddTo.Add(new Border
        {
            StrokeThickness = StrokeSize,
            Content = new Label
            {
                Text = entryText,
                BackgroundColor = BgColor,
                HeightRequest = Heights - 2 * StrokeSize,
                WidthRequest = Widths - 2 * StrokeSize,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, col, row);
    }


    private void OnGridTapped(Point pos)
    {
        _lastGridPoint = pos;
        var entryI = (int)(pos.X / Widths);
        var entryJ = (int)(pos.Y / Heights);
        _grid.Remove(_selection);
        _grid.Add(_selection, entryI, entryJ);

        var cellName = GetCellName(entryI, entryJ);
        var cellVal = _spreadsheet.GetCellValue(cellName);
        var entryText = _spreadsheet.GetCellContents(cellName, true).ToString() ?? "";

        CellInfoName.Text = "Selected: " + cellName;
        CellInfoValue.Text = "Value: " + cellVal;
        CellInfoContent.Text = "Content: " + entryText;

        var entry = new Entry
        {
            Text = entryText,
            BackgroundColor = Bg2Color,
            HeightRequest = Heights - 4 * StrokeSize,
            WidthRequest = Widths - 4 * StrokeSize,
            ClearButtonVisibility = ClearButtonVisibility.Never,
            CursorPosition = entryText.Length
        };
        var entryWithBorder = new Border
        {
            StrokeThickness = StrokeSize,
            Content = entry,
            ZIndex = 3
        };

        var hasOld = _labels.Remove(cellName, out var oldLabel);
        if (hasOld) _grid.Remove(oldLabel);
        
        
        _destroyCurrent();
        _destroyCurrent = () =>
        {
            _grid.Remove(entryWithBorder);
            if (hasOld) _grid.Add(oldLabel, entryI, entryJ);
        };
        entry.TextChanged += (_, _) => { CellInfoContent.Text = "Content: " + entry.Text; };
        entry.Completed += (_, _) =>
        {
            _destroyCurrent();
            try
            {
                var deps = _spreadsheet.SetContentsOfCell(cellName, entry.Text ?? "");
                CellInfoValue.Text = "Value: " + _spreadsheet.GetCellValue(cellName);
                foreach (var dep in deps)
                    if (SetCellForDep(dep))
                        return;
            }
            catch (Exception error)
            {
                DisplayAlert("Error", error.Message, "OK");
                CellInfoContent.Text = "Content: " + _spreadsheet.GetCellContents(cellName);
                if (hasOld) _grid.Add(oldLabel, entryI, entryJ);
            }
        };


        _grid.Add(entryWithBorder, entryI, entryJ);
        entry.Focus();
    }

    private bool SetCellForDep(string dep)
    {
        var value = _spreadsheet.GetCellValue(dep);
        var valueString =
            value is FormulaError exception ? exception.Reason :
            value is string s ? s :
            value is double d ? d.ToString() :
            "Something went wrong";
        if (_labels.Remove(dep, out var oldDepLabel)) _grid.Remove(oldDepLabel);
        if (valueString.Length == 0) return true;
        var label = new Border
        {
            ZIndex = 3,
            StrokeThickness = StrokeSize,
            Content = new Label
            {
                Text = valueString,
                BackgroundColor = value is FormulaError ? Colors.Red : Bg2Color,
                TextColor = value is FormulaError ? Colors.White : Colors.Black,
                HeightRequest = Heights - 2 * StrokeSize,
                WidthRequest = Widths - 2 * StrokeSize,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        _grid.Add(
            label,
            RowFromCellName(dep),
            ColFromCellName(dep));
        _labels.Add(dep, label);
        return false;
    }

    private async Task<bool> NoRiskOrUserAcceptedRisk()
    {
        return !_spreadsheet.Changed ||
               await DisplayAlert(
                   "Potential Data Loss",
                   "This action will cause current contents to be lost, do you wish to continue?",
                   "Yes",
                   "No"
               );
    }

    private async void FileMenuNew(object sender, EventArgs e)
    {
        if (!await NoRiskOrUserAcceptedRisk()) return;
        try
        {
            _spreadsheet = new Spreadsheet(SS.Utility.IsValidVar, s => s.ToUpper(), "six");
            foreach (var cellName in _labels.Keys)
                if (_labels.Remove(cellName, out var label))
                    _grid.Remove(label);
            foreach (var cellName in _spreadsheet.GetNamesOfAllNonemptyCells()) SetCellForDep(cellName);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Something went wrong", ex.ToString(), "OK");
        }
    }


    //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups?view=net-maui-8.0
    private async void FileMenuOpenAsync(object sender, EventArgs e)
    {
        if (!await NoRiskOrUserAcceptedRisk()) return;
        var filepath = await FilePicker.Default.PickAsync();
        try
        {
            _spreadsheet = new Spreadsheet(filepath.FullPath, SS.Utility.IsValidVar, s => s.ToUpper(), "six");
            foreach (var cellName in _labels.Keys)
                if (_labels.Remove(cellName, out var label))
                    _grid.Remove(label);
            foreach (var cellName in _spreadsheet.GetNamesOfAllNonemptyCells()) SetCellForDep(cellName);
        }
        catch (SpreadsheetReadWriteException ex)
        {
            await DisplayAlert("Failed to open file", ex.ToString(), "OK");
        }
    }

    private async void FileMenuHelp(object sender, EventArgs e)
    {
        var HelpText = "To create a new Spreadsheet, use the New button in the File Menu.\n" +
                       "To Open a previously made Spreadsheet, use the Open button in the File Menu.\n" +
                       "To View this help menu, use the Help button in the File Menu.\n" +
                       "To Export this Spreadsheet as a DLL to your Desktop, use the Export button in the File Menu.\n" +
                       "To Save this Spreadsheet to your local AppData folder to be used later, use the Save button in the File Menu.\n" +
                       "To create a new cell, click anywhere in the space between the labels. This will cause it to create a cell at the spot you clicked." +
                       " Once you type your contents in and press enter, it will finalize it and display the cell's value. When a cell " +
                       "turns green, it's contents is valid and when it turns red, it's contents is invalid and needs to be changed.";
        await DisplayAlert("Help",
            HelpText,
            "OK");
    }

    private async void FileMenuSaveAsync(object sender, EventArgs e)
    {
        if (FilenameAfterSave != null)
        {
            _spreadsheet.Save(FilenameAfterSave);
            return;
        }

        var filename = FileSystem.Current.AppDataDirectory + "\\" + await DisplayPromptAsync(
            "Save File",
            "Give a name to the Spreadsheet to be saved."
        ) + ".sprd";
        if (File.Exists(filename))
        {
            var save = await DisplayAlert(
                "Potential Data Loss",
                "This action will override the previous file's contents, do you wish to continue?",
                "Yes",
                "No"
            );
            if (save)
            {
                _spreadsheet.Save(filename);
                FilenameAfterSave = filename;
            }
        }
        else
        {
            _spreadsheet.Save(filename);
        }
    }

    private async void FileMenuExport(object sender, EventArgs e)
    {
        var filename = await DisplayPromptAsync(
            "Export File",
            "Give a name to the file to be exported as a dll to your desktop."
        );
        _spreadsheet.Compile(filename);
    }


    private static string ColName(int i)
    {
        return i.ToString();
    }

    private static string RowName(int i)
    {
        return "" + (char)('A' + i);
    }

    private static string GetCellName(int r, int c)
    {
        return RowName(r) + ColName(c);
    }

    private static int RowFromCellName(string name)
    {
        return name[0] - 'A';
    }

    private static int ColFromCellName(string name)
    {
        return int.Parse(name[1..]);
    }
}