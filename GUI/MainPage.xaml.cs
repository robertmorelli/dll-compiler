using System.Linq;

namespace GUI
{
    public partial class MainPage : ContentPage
    {
        static readonly int rows = 80000;
        static readonly int columns = 80000;
        public MainPage()
        {
            InitializeComponent();
            VerticalStackLayout gridholder = FindByName("Grid") as VerticalStackLayout;
            Grid grid = new()
            {
                RowDefinitions = new RowDefinitionCollection(
                    Enumerable.Range(0, rows)
                    .Select((_) => new RowDefinition(height: 10)).ToArray()), //.Where<RowDefinition>((_) => new RowDefinition()).ToArray(),
                ColumnDefinitions = new ColumnDefinitionCollection(
                    Enumerable.Range(0, columns)
                    .Select((_) => new ColumnDefinition(width: 10)).ToArray()),
                WidthRequest = columns * 10,
                BackgroundColor = Colors.Azure
            };
            TapGestureRecognizer taps = new();
            taps.Tapped += (s, e) => OnGridTapped(s, e, grid);
            grid.GestureRecognizers.Add(taps);

            gridholder?.Add(grid);
        }

        static private void OnGridTapped(object? _, TappedEventArgs e, Grid grid)
        {
            var pos = (Point)e.GetPosition(grid);
            grid.Add(
                new Label
                {
                    BackgroundColor = Colors.Red
                },
                (int)(pos.X / 10),
                (int)(pos.Y / 10));
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