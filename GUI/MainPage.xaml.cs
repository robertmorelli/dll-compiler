using SpreadsheetUtilities;
using SS;

namespace GUI;

public partial class MainPage : ContentPage
{
    private static readonly int rows = 7;
    private static readonly int columns = 7;
    private static readonly int widths = 80;
    private static readonly int heights = 30;
    private static readonly Color BGColor = Colors.Green;
    private int entryI;
    private int entryJ;
    private Spreadsheet spreadsheet;

    public MainPage() //might need to change how this is loaded
    {
        var EmptyCorner = new Border
            { StrokeThickness = 2, HeightRequest = heights, WidthRequest = Height, BackgroundColor = BGColor };
        // grid code don't touch unless need to, instead I'll add it to a container
        spreadsheet = new Spreadsheet(s => true, s => s.ToUpper(), "six");
        InitializeComponent();


        //make labels
        var leftLabels = new VerticalStackLayout();
        var columnLabels = new HorizontalStackLayout();
        for (var i = 0; i < rows; i++) AddEntry(leftLabels, "" + i, 2);
        for (var i = 0; i < columns; i++) AddEntry(columnLabels, "" + (char)('A' + i), 2);

        //definitions for grid
        var rowsArray = new RowDefinition[rows];
        var colsArray = new ColumnDefinition[columns];
        for (var i = 0; i < rows; i++) rowsArray[i] = new RowDefinition(heights);
        for (var i = 0; i < columns; i++) colsArray[i] = new ColumnDefinition(widths);
        var rowsDef = new RowDefinitionCollection(rowsArray);
        var colsDef = new ColumnDefinitionCollection(colsArray);

        Grid grid = new()
        {
            RowDefinitions = rowsDef,
            ColumnDefinitions = colsDef,
            WidthRequest = columns * widths,
            HeightRequest = rows * heights,
            BackgroundColor = BGColor
        };

        var taps = new TapGestureRecognizer();
        taps.Tapped += (_, e) => OnGridTapped(e, grid);
        grid.GestureRecognizers.Add(taps);

        Container.Add(EmptyCorner, 0);
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
                BackgroundColor = BGColor,
                Text = entryText,
                HeightRequest = heights,
                WidthRequest = heights,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        });
    }


    private void OnGridTapped(TappedEventArgs e, Grid grid)
    {
        var pos = (Point)e.GetPosition(grid);
        entryI = (int)(pos.X / widths);
        entryJ = (int)(pos.Y / heights);
        var cellName = getCellName(entryI, entryJ);

        var entryText = spreadsheet.GetCellContents(cellName, true).ToString() ?? "";
        var entry = new Entry
        {
            BackgroundColor = BGColor,
            Text = entryText,
            ClearButtonVisibility = ClearButtonVisibility.Never,
            CursorPosition = entryText.Length,
            
        };


        entry.Unfocused += (_, _) =>
        {
            try
            {
                var toDo = spreadsheet.SetContentsOfCell(cellName, entry.Text ?? "");
                foreach (var item in toDo)
                {
                    var value = spreadsheet.GetCellValue(item);
                    var valueString =
                        value is FormulaError exception ? exception.Reason :
                        value is string s ? s :
                        value is double d ? d.ToString("F") :
                        "Something went wrong";
                    if (!valueString.Equals(""))
                    {
                        var lab = new Label
                        {
                            BackgroundColor = BGColor,
                            Text = valueString
                        };
                        grid.Add(lab, RowFromCellName(item), ColFromCellName(item));
                    }
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

    private async void FileMenuNew(object sender, EventArgs e)
    {
        if (spreadsheet.Changed)
        {
            var response = await DisplayAlert("Potential Data Loss",
                "Creating a new file will cause current contents to be lost, do you wish to continue?",
                "Yes", "No");
            if (response) spreadsheet = new Spreadsheet(); //curious if it can imply the same parameters
        }
        else
        {
            spreadsheet = new Spreadsheet(); //do we need to do other initalization?
        }
    }

    private async void
        FileMenuOpenAsync(object sender,
            EventArgs e) //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups?view=net-maui-8.0
    {
        if (spreadsheet.Changed)
        {
            var response = await DisplayAlert("Potential Data Loss",
                "Opening a new file will cause current contents to be lost, do you wish to continue?",
                "Yes", "No");
            if (response) spreadsheet = new Spreadsheet();

            var filepath = await DisplayPromptAsync("Open File",
                "Give the path to the file to be opened.");
            try
            {
                spreadsheet = new Spreadsheet(filepath, s => true, s => s.ToUpper(), "six");
            }
            catch (SpreadsheetReadWriteException ex)
            {
                await DisplayAlert("Failed to open file", ex.ToString(), "OK");
            }
        }
        else
        {
            //do we need to do other initalization? Also I need some ways to shorten this.

            var filepath = await DisplayPromptAsync("Open File",
                "Give the path to the file to be opened.");
            try
            {
                spreadsheet = new Spreadsheet(filepath, s => true, s => s.ToUpper(), "six");
            }
            catch (SpreadsheetReadWriteException ex)
            {
                await DisplayAlert("Failed to open file", ex.ToString(), "OK");
            }
        }
    }

    private void FileMenuHelp(object sender, EventArgs e)
    {
    }


    private string ColName(int i)
    {
        return i.ToString();
    }

    private string RowName(int i)
    {
        return "" + (char)('A' + i);
    }

    private string getCellName(int r, int c)
    {
        return RowName(r) + ColName(c);
    }

    private int RowFromCellName(string name)
    {
        return name[0] - 'A';
    }

    private int ColFromCellName(string name)
    {
        return int.Parse(name[1..]);
    }
}