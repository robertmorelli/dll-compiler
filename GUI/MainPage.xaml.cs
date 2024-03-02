using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Maui.Controls.Internals;
using SpreadsheetUtilities;
using SS;

namespace GUI
{
    public partial class MainPage : ContentPage
    {
        static readonly int rows = 7;
        static readonly int columns = 7;
        static readonly int widths = 80;
        static readonly int heights = 80;
        private Spreadsheet spreadsheet;
        private int entryI = 0;
        private int entryJ = 0;
        static readonly Color BGColor = Colors.DarkKhaki;
        public MainPage() //might need to change how this is loaded
        {

            // grid code don't touch unless need to, instead I'll add it to a container
            spreadsheet = new(s => true, s => s.ToUpper(), "six");
            InitializeComponent();

            VerticalStackLayout leftlabels = new VerticalStackLayout{};
            for (int i = 0; i < rows; i++)
            {
                AddEntry(leftlabels, i.ToString(), 2);
            }

            HorizontalStackLayout columnlabels = new HorizontalStackLayout{};
            for (int i = 0; i < columns; i++)
            {
                AddEntry(columnlabels, ((char)('A'+i)).ToString(), 2);
            }

            Border EmptyCorner = new Border{StrokeThickness = 2, HeightRequest = heights, WidthRequest = Height, BackgroundColor = BGColor};

            Grid grid = new()
            {
                RowDefinitions = new RowDefinitionCollection(
                    Enumerable.Range(0, rows)
                    .Select((_) => new RowDefinition(height: heights)).ToArray()), //.Where<RowDefinition>((_) => new RtaowDefinition()).ToArray(),
                ColumnDefinitions = new ColumnDefinitionCollection(
                    Enumerable.Range(0, columns)
                    .Select((_) => new ColumnDefinition(width: widths)).ToArray()),
                
                WidthRequest = columns * widths,
                HeightRequest = rows * heights,
                BackgroundColor = BGColor,
            };

            TapGestureRecognizer taps = new();
            taps.Tapped += (_, e) => OnGridTapped(e, grid);
            grid.GestureRecognizers.Add(taps);

            Container.Add(EmptyCorner, 0, 0);
            Container.Add(leftlabels, 0, 1);
            Container.Add(grid, 1, 1);
            Container.Add(columnlabels, 1, 0);

        }


        private void AddEntry(Layout layoutToAddTo, string entryText, int strokeSize) 
        {
            var border = new Border
            {   
                StrokeThickness = strokeSize,
                Content = new Label
                {
                    BackgroundColor = BGColor,
                    Text = entryText.ToString(),
                    HeightRequest = heights,
                    WidthRequest = heights,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            };
            layoutToAddTo.Add(border);
        }

        private void OnGridTapped(TappedEventArgs e, Grid grid)
        {
            var pos = (Point)e.GetPosition(grid);
            entryI = (int)(pos.X / widths);
            entryJ = (int)(pos.Y / heights);
            string cellName = getCellName(entryI, entryJ);


            Entry entry = new Entry
            {
                BackgroundColor = BGColor,
                Text = spreadsheet.GetCellContents(cellName, true).ToString(),
                ClearButtonVisibility = ClearButtonVisibility.Never,
            };


            entry.Unfocused += (sender, e) =>
            {
                try
                {
                    var toDo = spreadsheet.SetContentsOfCell(cellName, entry.Text ?? "");
                    foreach (var item in toDo)
                    {
                        var value = spreadsheet.GetCellValue(item);
                        var valueString =
                            (value is FormulaError exception) ? exception.Reason :
                            (value is string s) ? s :
                            (value is double d) ? d.ToString("F") :
                            "Something went wrong";
                        if (!valueString.Equals(""))
                        {
                            var lab = new Label()
                            {
                                BackgroundColor = BGColor,
                                Text = valueString,
                            };
                            grid.Add(lab, RowFromCellName(item), ColFromCellName(item));
                        }
                    }

                }
                catch (Exception error)
                {
                    DisplayAlert("Error", error.Message, "OK");
                    //manage errors for circular ...
                }
                finally
                {
                    grid.Remove(entry);
                }
            };
            entry.Completed += (sender, e) => entry.Unfocus();

            grid.Add(entry, entryI, entryJ);
            entry.Focus();
            entry.CursorPosition = (entry.Text ?? "").Length;
            entry.ClearButtonVisibility = ClearButtonVisibility.Never;
        }

        private void ChangeCell(int i, int j)
        {

        }

        private async void FileMenuNew(object sender, EventArgs e)
        {
            if (spreadsheet.Changed)
            {
                bool response = await DisplayAlert("Potential Data Loss",
                "Creating a new file will cause current contents to be lost, do you wish to continue?",
                "Yes", "No");
                if (response) spreadsheet = new(); //curious if it can imply the same parameters
            }
            else spreadsheet = new(); //do we need to do other initalization?
        }

        private async void FileMenuOpenAsync(object sender, EventArgs e) //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/pop-ups?view=net-maui-8.0
        {
            if (spreadsheet.Changed)
            {
                bool response = await DisplayAlert("Potential Data Loss",
                "Opening a new file will cause current contents to be lost, do you wish to continue?",
                "Yes", "No");
                if (response) spreadsheet = new();

                string filepath = await DisplayPromptAsync("Open File",
                "Give the path to the file to be opened.");
                try
                {
                    spreadsheet = new(filepath, s => true, s => s.ToUpper(), "six");
                }
                catch (SpreadsheetReadWriteException ex)
                {
                    await DisplayAlert("Failed to open file", ex.ToString(), "OK");
                }
            }
            else
            { //do we need to do other initalization? Also I need some ways to shorten this.

                string filepath = await DisplayPromptAsync("Open File",
                    "Give the path to the file to be opened.");
                try
                {
                    spreadsheet = new(filepath, s => true, s => s.ToUpper(), "six");
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



        private string ColName(int i) => i.ToString();
        private string RowName(int i) => "" + (char)('A' + i);
        private string getCellName(int r, int c) => RowName(r) + ColName(c);

        private int RowFromCellName(string name) => name[0] - 'A';
        private int ColFromCellName(string name) => int.Parse(name[1..]);

    }
}



/*for (int i = 0; i < rows; i++)
    for (int j = 0; j < columns; j++)
        grid.Add(
            new Label
            {
                Text = $"Row {i}, Col {j}",
                BackgroundColor = Colors.Azure
            }, j, i);*/
/*
 * 
 * 
 * 

*/