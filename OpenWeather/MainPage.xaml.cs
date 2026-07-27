using OpenWeather.ViewModels;
namespace OpenWeather;
public partial class MainPage : ContentPage
{
    public MainPage(WeatherViewModel viewModel) { InitializeComponent(); BindingContext = viewModel; }
}
