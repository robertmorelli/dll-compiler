using SpreadsheetUtilities;
using SS;

namespace GUI;

public partial class MainPage : ContentPage
{
    private const int Rows = 7;
    private const int Columns = 70;
    private const int Widths = 200;
    private const int Heights = 30;
    private static readonly Color BgColor = Colors.Lavender;
    private readonly Grid _grid;

    private readonly Dictionary<string, Label> _labels = [];
    private Spreadsheet _spreadsheet;

    public MainPage() //might need to change how this is loaded
    {
        InitializeComponent();
        _spreadsheet = new Spreadsheet(s => true, s => s.ToUpper(), "six");


        //make labels
        var leftLabels = new VerticalStackLayout();
        var columnLabels = new HorizontalStackLayout();
        for (var i = 0; i < Rows; i++) AddEntry(leftLabels, "" + i, 2);
        for (var i = 0; i < Columns; i++) AddEntry(columnLabels, "" + (char)('A' + i), 2);

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
        for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Columns; j++)
            _grid.Add(
                new Label
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0,0),
                        EndPoint = new Point(1,0),
                        GradientStops = [
                            new GradientStop
                            {
                                Color = Colors.Black,
                                Offset = 0f
                            },
                            new GradientStop
                            {
                                Color = Colors.Black,
                                Offset = .0199f
                            },
                            new GradientStop
                            {
                                Color = Colors.White,
                                Offset = .02f
                            },
                            new GradientStop
                            {
                                Color = Colors.White,
                                Offset = .9799f
                            },
                            new GradientStop
                            {
                                Color = Colors.Black,
                                Offset = .98f
                            },
                            new GradientStop
                            {
                                Color = Colors.Black,
                                Offset = 1f
                            },
                        ]
                    }
                },
                j,
                i
            );


        var taps = new TapGestureRecognizer();
        taps.Tapped += (_, e) => OnGridTapped(e);
        _grid.GestureRecognizers.Add(taps);

        var emptyCorner = new Border
        {
            StrokeThickness = 2,
            HeightRequest = Heights,
            WidthRequest = Height,
            BackgroundColor = BgColor
        };

        Container.Add(emptyCorner, 0);
        Container.Add(leftLabels, 0, 1);
        Container.Add(_grid, 1, 1);
        Container.Add(columnLabels, 1);
    }


    private static void AddEntry(Layout layoutToAddTo, string entryText, int strokeSize)
    {
        layoutToAddTo.Add(new Border
        {
            StrokeThickness = strokeSize,
            Content = new Label
            {
                BackgroundColor = BgColor,
                Text = entryText,
                HeightRequest = Heights,
                WidthRequest = Heights,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        });
    }


    private void OnGridTapped(TappedEventArgs e)
    {
        var pos = (Point)e.GetPosition(_grid);
        var entryI = (int)(pos.X / Widths);
        var entryJ = (int)(pos.Y / Heights);
        var cellName = GetCellName(entryI, entryJ);

        var entryText = _spreadsheet.GetCellContents(cellName, true).ToString() ?? "";
        var entry = new Entry
        {
            BackgroundColor = Colors.Transparent,
            Text = entryText,
            ClearButtonVisibility = ClearButtonVisibility.Never,
            CursorPosition = entryText.Length
        };
        var hasOld = _labels.Remove(cellName, out var oldLabel);
        if (hasOld) _grid.Remove(oldLabel);


        entry.Unfocused += (_, _) =>
        {
            try
            {
                var deps = _spreadsheet.SetContentsOfCell(cellName, entry.Text ?? "");
                foreach (var dep in deps)
                {
                    var value = _spreadsheet.GetCellValue(dep);
                    var valueString =
                        value is FormulaError exception ? exception.Reason :
                        value is string s ? s :
                        value is double d ? d.ToString("F") :
                        "Something went wrong";
                    if (_labels.Remove(dep, out var oldDepLabel)) _grid.Remove(oldDepLabel);
                    if (valueString.Length == 0) return;
                    var label = new Label
                    {
                        BackgroundColor = Colors.Transparent,
                        Text = valueString
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
            finally
            {
                _grid.Remove(entry);
            }
        };
        entry.Completed += (_, _) => entry.Unfocus();
        _grid.Add(entry, entryI, entryJ);
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
            _spreadsheet = new Spreadsheet(s => true, s => s.ToUpper(), "six");
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
            _spreadsheet = new Spreadsheet(filepath, s => true, s => s.ToUpper(), "six");
            foreach (var cellName in _labels.Keys)
                if (_labels.Remove(cellName, out var label))
                    _grid.Remove(label);
        }
        catch (SpreadsheetReadWriteException ex)
        {
            await DisplayAlert("Failed to open file", ex.ToString(), "OK");
        }
    }

    private void FileMenuHelp(object sender, EventArgs e)
    {

    }

    /*private async void FileMenuExport(object sender, EventArgs e) 
    {
        var filename = await DisplayPromptAsync(
            "Export File",
            "Give a name to the file to be exported as a dll to your desktop."
            );
        _spreadsheet.Compile?(filename);
    }*/

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