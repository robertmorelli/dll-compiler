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
        public MainPage()
        {
            spreadsheet = new();
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
                BackgroundColor = Colors.SkyBlue
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
                BackgroundColor = Colors.Azure,
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
                                BackgroundColor = Colors.Azure,
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

        private void FileMenuNew(object sender, EventArgs e)
        {

            //if (spreadsheet.Changed)
            //else spreadsheet.Save() { }
        }

        private void FileMenuOpenAsync(object sender, EventArgs e)
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