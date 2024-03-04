using SpreadsheetUtilities;
using SS;

namespace GUI;

public partial class MainPage : ContentPage
{
    private const int Rows = 20;
    private const int Columns = 26;
    private const int Widths = 200;
    private const int Heights = 35;
    private const int StrokeSize = 1;
    private static readonly Color BgColor = Colors.Lavender;
    
    private static readonly Color Bg2Color = Colors.Azure;

    private static readonly Color Bg3Color = new Color(150,200,180);

    private readonly Grid _grid;


    private readonly Dictionary<string, Border> _labels = [];
    private Spreadsheet _spreadsheet;

    public MainPage() //might need to change how this is loaded
    {
        InitializeComponent();
        _spreadsheet = new Spreadsheet(Utility.IsValidVar, s => s.ToUpper(), "six");
        nullCell.WidthRequest = Widths;
        


        //definitions for grid
        var rowsArray = new RowDefinition[Rows];
        var colsArray = new ColumnDefinition[Columns];
        for (var i = 0; i < Rows; i++) rowsArray[i] = new RowDefinition(Heights);
        for (var i = 0; i < Columns; i++) colsArray[i] = new ColumnDefinition(Widths);
        var rowsDef = new RowDefinitionCollection(rowsArray);
        var colsDef = new ColumnDefinitionCollection(colsArray);


        _grid = new Grid
        {
            RowDefinitions = rowsDef,
            ColumnDefinitions = colsDef,
            WidthRequest = Columns * Widths,
            HeightRequest = Rows * Heights
        };

        //for (var i = 0; i < Rows; i++)
        //for (var j = 0; j < Columns; j++)
        //    _grid.Add(new Border(), j, i);


        var taps = new TapGestureRecognizer();
        taps.Tapped += (_, e) => OnGridTapped(e);
        _grid.GestureRecognizers.Add(taps);
        Grid.Content = _grid;


        colsDef.Add(colsArray[0]);
        //make labels
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
        for (var i = 0; i < Rows; i++) AddEntry(leftLabels, "" + i, i, 0);
        for (var i = 0; i < Columns; i++) AddEntry(columnLabels, "" + (char)('A' + i), 0, i);

        TopLabels.Content = columnLabels;
        LeftLabels.Content = leftLabels;

        Grid.Scrolled += (_, e) =>
        {
            TopLabels.ScrollToAsync(e.ScrollX, TopLabels.ScrollY, true);
        };
        TopLabels.Scrolled += (_, e) =>
        {
            Grid.ScrollToAsync(e.ScrollX, Grid.ScrollY, true);
        };

        var rowBar = new Label();
        var colBar = new Label();
        var hover = new PointerGestureRecognizer();
        hover.PointerMoved += (sender, e) =>
        {
            var pos = (Point)e.GetPosition(_grid);
            var entryI = (int)(pos.X / Widths);
            var entryJ = (int)(pos.Y / Heights);
            var cellName = GetCellName(entryI, entryJ);

            _grid.Remove(rowBar);
            _grid.Remove(colBar);
            rowBar = new Label{BackgroundColor = new Color(0,0,0,10),ZIndex = 2, WidthRequest = Grid.Width};
            colBar = new Label{BackgroundColor = new Color(0,0,0,10),ZIndex = 2, HeightRequest = Grid.Height};

            HoverCell.Text = "Hover: " + cellName;
            _grid.Add(rowBar,  0, Rows + 1, entryJ, entryJ + 1);
            _grid.Add(colBar, entryI, entryI + 1,0,Columns + 1 );
        };
        hover.PointerExited += (_,_) => {
            _grid.Remove(rowBar);
            _grid.Remove(colBar); 
            HoverCell.Text = "Hover: " + "--";
        };
        _grid.GestureRecognizers.Add(hover);



        this.SizeChanged += (sender, args) => {
            var width = this.Width;
            var height = this.Height;
            Entire.WidthRequest = width;
            Entire.HeightRequest = height;
            Border.WidthRequest = width - StrokeSize;
            TopLabelsHolder.WidthRequest = width - StrokeSize;
            TopLabels.WidthRequest = width - Widths - StrokeSize;
            Table.WidthRequest = width - StrokeSize;
            Table.HeightRequest = height - Heights;
            Grid.HeightRequest = Math.Min(Table.HeightRequest - Heights,Heights * Rows);
            Grid.WidthRequest = width - Widths - StrokeSize;
        };

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


    private void OnGridTapped(TappedEventArgs e)
    {
        var pos = (Point)e.GetPosition(_grid);
        var entryI = (int)(pos.X / Widths);
        var entryJ = (int)(pos.Y / Heights);

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


        entry.Unfocused += (_, _) =>
        {
            _grid.Remove(entryWithBorder);
            try
            {
                var deps = _spreadsheet.SetContentsOfCell(cellName, entry.Text ?? "");
                foreach (var dep in deps)
                {
                    var value = _spreadsheet.GetCellValue(dep);
                    var valueString =
                        value is FormulaError exception ? exception.Reason :
                        value is string s ? s :
                        value is double d ? d.ToString("N") :
                        "Something went wrong";
                    if (_labels.Remove(dep, out var oldDepLabel)) _grid.Remove(oldDepLabel);
                    if (valueString.Length == 0) return;
                    var label = new Border
                    {
                        ZIndex = 3,
                        StrokeThickness = StrokeSize,
                        Content = new Label
                        {
                            Text = valueString,
                            BackgroundColor = (value is FormulaError)?Colors.PaleVioletRed:Bg3Color,
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
                }
            }
            catch (Exception error)
            {
                DisplayAlert("Error", error.Message, "OK");
                if (hasOld) _grid.Add(oldLabel, entryI, entryJ);
            }
        };
        entry.Completed += (_, _) => entry.Unfocus();
        _grid.Add(entryWithBorder, entryI, entryJ);
        entry.Focus();
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
            _spreadsheet = new Spreadsheet(Utility.IsValidVar, s => s.ToUpper(), "six");
            foreach (var cellName in _labels.Keys)
                if (_labels.Remove(cellName, out var label))
                    _grid.Remove(label);
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
        var filepath = await DisplayPromptAsync(
            "Open File",
            "Give the path to the file to be opened."
        );
        try
        {
            _spreadsheet = new Spreadsheet(filepath, Utility.IsValidVar, s => s.ToUpper(), "six");
            foreach (var cellName in _labels.Keys)
                if (_labels.Remove(cellName, out var label))
                    _grid.Remove(label);
        }
        catch (SpreadsheetReadWriteException ex)
        {
            await DisplayAlert("Failed to open file", ex.ToString(), "OK");
        }
    }

    private async void FileMenuHelp(object sender, EventArgs e)
    {
        var HelpText = "To create a new Spreadsheet, use the New button in the File Menu." +
            "To Open a previously made Spreadsheet, use the Open button in the File Menu." +
            "To View this help menu, use the Help button in the File Menu" +
            "To Export this Spreadsheet as a DLL to your Desktop, use the Export button in the File Menu." +
            "To Save this Spreadsheet to be used later, use the Save button in the File Menu";
        await DisplayAlert("Help",
            HelpText,
            "OK");
    }


    private async void FileMenuExport(object sender, EventArgs e)
    {
        var filename = await DisplayPromptAsync(
            "Export File",
            "Give a name to the file to be exported as a dll to your desktop."
        );
        /*_spreadsheet.Compile?(filename);}*/
    }

    private async void FileMenuSave(object sender, EventArgs e)
    {
        var filename = await FilePicker.Default.PickAsync();
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