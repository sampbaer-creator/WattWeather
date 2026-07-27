using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Models;
namespace OpenWeather.ViewModels;
public partial class WeatherViewModel(IWeatherService weatherService) : ObservableObject
{
 [ObservableProperty] private string query="Denver, CO";
 [ObservableProperty] private bool isBusy;
 [ObservableProperty] private string? errorMessage;
 [ObservableProperty] private WeatherSnapshot? weather;
 public bool HasWeather=>Weather is not null;
 public string LocationLabel=>Weather is null?"":$"{Weather.City.ToUpperInvariant()}, {Weather.Region??Weather.CountryCode}";
 public string TemperatureLabel=>Weather is null?"—":$"{Weather.TemperatureF:0}°";
 public string DescriptionLabel=>Weather is null?"":$"{Weather.Description} · Feels like {Weather.FeelsLikeF:0}°";
 public string HighLowLabel=>Weather is null?"":$"High / low  {Weather.HighTemperatureF:0}° / {Weather.LowTemperatureF:0}°";
 public string HumidityLabel=>Weather is null?"":$"Humidity  {Weather.HumidityPercent:0}%";
 public string WindLabel=>Weather is null?"":$"Wind  {Weather.WindSpeedMph:0.0} mph";
 public string SunLabel=>Weather is null?"":$"Sun  {Weather.Sunrise:h:mm tt}–{Weather.Sunset:h:mm tt}";
 partial void OnWeatherChanged(WeatherSnapshot? value){OnPropertyChanged(nameof(HasWeather));OnPropertyChanged(nameof(LocationLabel));OnPropertyChanged(nameof(TemperatureLabel));OnPropertyChanged(nameof(DescriptionLabel));OnPropertyChanged(nameof(HighLowLabel));OnPropertyChanged(nameof(HumidityLabel));OnPropertyChanged(nameof(WindLabel));OnPropertyChanged(nameof(SunLabel));}
 [RelayCommand] private async Task SearchAsync(){if(IsBusy)return;IsBusy=true;ErrorMessage=null;try{Weather=await weatherService.GetCurrentAsync(Query);}catch(Exception ex){ErrorMessage=ex.Message;}finally{IsBusy=false;}}
}
