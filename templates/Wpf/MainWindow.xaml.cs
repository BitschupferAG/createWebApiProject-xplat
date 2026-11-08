using System.Net.Http;
using System.Windows;

namespace {{NAMESPACE}};

public partial class MainWindow : Window
{
  private static readonly HttpClient Http = new() { BaseAddress = new Uri("{{BASEURL}}") };

  public MainWindow() => InitializeComponent();

  private async void Window_Loaded(object sender, RoutedEventArgs e) => await TestBackendAsync();

  private async void BtnTest_Click(object sender, RoutedEventArgs e) => await TestBackendAsync();

  private async Task TestBackendAsync()
  {
    try
    {
      txtResult.Text = await Http.GetStringAsync("{{ROUTE}}");
      Title = "Backend erreichbar";
    }
    catch (Exception ex)
    {
      Title = "Backend nicht erreichbar";
      txtResult.Text = ex.Message;
    }
  }
}
