using SpreadsheetUtilities;
using SS;

namespace GUI;

public partial class MainPage : ContentPage
{
    private const int Rows = 7;
    private const int Columns = 7;
    private const int Widths = 80;
    private const int Heights = 30;
    private static readonly Color BgColor = Colors.Green;

    private Dictionary<string, Label> _labels = [];
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

        Grid grid = new()
        {
            RowDefinitions = rowsDef,
            ColumnDefinitions = colsDef,
            WidthRequest = Columns * Widths,
            HeightRequest = Rows * Heights,
            BackgroundColor = BgColor
        };

        var taps = new TapGestureRecognizer();
        taps.Tapped += (_, e) => OnGridTapped(e, grid);
        grid.GestureRecognizers.Add(taps);

        var emptyCorner = new Border
        {
            StrokeThickness = 2,
            HeightRequest = Heights,
            WidthRequest = Height,
            BackgroundColor = BgColor
        };

        Container.Add(emptyCorner, 0);
        Container.Add(leftLabels, 0, 1);
        Container.Add(grid, 1, 1);
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


    private void OnGridTapped(TappedEventArgs e, Grid grid)
    {
        var pos = (Point)e.GetPosition(grid);
        var entryI = (int)(pos.X / Widths);
        var entryJ = (int)(pos.Y / Heights);
        var cellName = GetCellName(entryI, entryJ);

        var entryText = _spreadsheet.GetCellContents(cellName, true).ToString() ?? "";
        var entry = new Entry
        {
            BackgroundColor = BgColor,
            Text = entryText,
            ClearButtonVisibility = ClearButtonVisibility.Never,
            CursorPosition = entryText.Length
        };


        entry.Unfocused += (_, _) =>
        {
            try
            {
                var deps = _spreadsheet.SetContentsOfCell(cellName, entryText);
                foreach (var dep in deps)
                {
                    var value = _spreadsheet.GetCellValue(dep);
                    var valueString =
                        value is FormulaError exception ? exception.Reason :
                        value is string s ? s :
                        value is double d ? d.ToString("F") :
                        "Something went wrong";
                    if (valueString.Length == 0) return;
                    grid.Add(
                        new Label
                        {
                            BackgroundColor = BgColor,
                            Text = valueString
                        },
                        RowFromCellName(dep),
                        ColFromCellName(dep));
                }
            }
            catch (Exception error)
            {
                DisplayAlert("Error", error.Message, "OK");
            }
            finally
            {
                grid.Remove(entry);
            }
        };
        entry.Completed += (_, _) => entry.Unfocus();
        grid.Add(entry, entryI, entryJ);
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
            //destroy all labels
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
            //destroy all labels
        }
        catch (SpreadsheetReadWriteException ex)
        {
            await DisplayAlert("Failed to open file", ex.ToString(), "OK");
        }
    }

    private void FileMenuHelp(object sender, EventArgs e)
    {
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