using System.Linq;

namespace GUI
{
    public partial class MainPage : ContentPage
    {
        static readonly int rows = 1_000_000;
        static readonly int columns = 1_000_000;
        static readonly int widths = 400;
        static readonly int height = 30;
        public MainPage()
        {
            InitializeComponent();
            VerticalStackLayout gridholder = FindByName("Grid") as VerticalStackLayout;
            Grid grid = new()
            {
                RowDefinitions = new RowDefinitionCollection(
                    Enumerable.Range(0, rows)
                    .Select((_) => new RowDefinition(height: height)).ToArray()), //.Where<RowDefinition>((_) => new RowDefinition()).ToArray(),
                ColumnDefinitions = new ColumnDefinitionCollection(
                    Enumerable.Range(0, columns)
                    .Select((_) => new ColumnDefinition(width: widths)).ToArray()),
                WidthRequest = columns * widths,
                HeightRequest = rows * height,
                BackgroundColor = Colors.Azure
            };
            TapGestureRecognizer taps = new();
            taps.Tapped += (s, e) => OnGridTapped(e, grid);
            grid.GestureRecognizers.Add(taps);

            gridholder?.Add(grid);
        }

        static private void OnGridTapped(TappedEventArgs e, Grid grid)
        {
            var pos = (Point)e.GetPosition(grid);
            var inp = new Entry
            {
                BackgroundColor = Colors.Beige,
                HeightRequest = height,
                WidthRequest = widths,
            };
            grid.Add(
                inp,
                (int)(pos.X / widths),
                (int)(pos.Y / height)
                );
            inp.Focus();
        }
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
 * <ContentPage.MenuBarItems>
        <MenuBarItem Text="File">
            <MenuFlyoutItem Text="New" Clicked="FileMenuNew" />
            <MenuFlyoutItem Text="Open" Clicked="FileMenuOpenAsync" />
        </MenuBarItem>
    </ContentPage.MenuBarItems>

*/