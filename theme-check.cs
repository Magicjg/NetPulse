using System;
using System.Windows;
using System.Windows.Media;
using NetPulse.App;
using NetPulse.App.Services;

var app = new App();
var settings = new AppSettingsService();
var theme = new ThemeService(app, settings);
Console.WriteLine($"Initial dark? {theme.IsDarkTheme}");
var bg1 = (SolidColorBrush)app.Resources["AppBackgroundBrush"];
Console.WriteLine($"Before: {bg1.Color}");
theme.SetTheme(false);
var bg2 = (SolidColorBrush)app.Resources["AppBackgroundBrush"];
Console.WriteLine($"After light: {bg2.Color}");
theme.SetTheme(true);
var bg3 = (SolidColorBrush)app.Resources["AppBackgroundBrush"];
Console.WriteLine($"After dark: {bg3.Color}");
