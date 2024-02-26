using Microsoft.Maui.Controls.Internals;
using SpreadsheetUtilities;
using SS;

namespace GUI
{
    public partial class MainPage : ContentPage
    {
        static readonly int rows = 99;
        static readonly int columns = 26;
        static readonly int widths = 400;
        static readonly int heights = 30;
        internal Spreadsheet spreadsheet;
        internal Dictionary<string, Label> labels = [];
        internal int entryI = 0;
        internal int entryJ = 0;
        public MainPage() //might need to change how this is loaded
        {
            spreadsheet = new(s => true, s => s.ToUpper(), "six");
            InitializeComponent();
            VerticalStackLayout gridholder = FindByName("Grid") as VerticalStackLayout;
            Grid grid = new()
            {
                RowDefinitions = new RowDefinitionCollection(
                    Enumerable.Range(0, rows)
                    .Select((_) => new RowDefinition(height: heights)).ToArray()), //.Where<RowDefinition>((_) => new RowDefinition()).ToArray(),
                ColumnDefinitions = new ColumnDefinitionCollection(
                    Enumerable.Range(0, columns)
                    .Select((_) => new ColumnDefinition(width: widths)).ToArray()),
                WidthRequest = columns * widths,
                HeightRequest = rows * heights,
                BackgroundColor = Colors.Black
            };

            TapGestureRecognizer taps = new();
            taps.Tapped += (_, e) => OnGridTapped(e, grid);
            grid.GestureRecognizers.Add(taps);


            gridholder?.Add(grid);
        }

        private void OnGridTapped(TappedEventArgs e, Grid grid)
        {
            var pos = (Point)e.GetPosition(grid);
            entryI = (int)(pos.X / widths);
            entryJ = (int)(pos.Y / heights);
            string cellName = getCellName(entryI, entryJ);


            if (labels.TryGetValue(cellName, out var label))
            {
                grid.Remove(label);
                labels.Remove(cellName);
            }

            var entry = new Entry
            {
                BackgroundColor = Colors.Black,
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
                        if (labels.TryGetValue(item, out var label))
                        {
                            grid.Remove(label);
                            labels.Remove(item);
                        }
                        object value = spreadsheet.GetCellValue(item);
                        string valueString =
                            (value is FormulaError exception) ?
                                exception.Reason :
                                (value is string s) ?
                                    s :
                                    (value is double d) ?
                                    d.ToString() :
                                    "Something went wrong";
                        if (!valueString.Equals(""))
                        {
                            var lab = new Label
                            {
                                BackgroundColor = Colors.Black,
                                Text = valueString
                            };
                            labels.Add(item, lab);
                            grid.Add(lab, RowFromCellName(item), ColFromCellName(item));
                        }
                    }
                    grid.Remove(entry);
                }
                catch
                {
                    //manage errors for circular ...
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