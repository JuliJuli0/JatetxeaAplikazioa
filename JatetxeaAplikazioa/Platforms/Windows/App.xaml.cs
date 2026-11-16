using Microsoft.UI.Xaml;

// Windows-eko hasierako klasea. Namespace-a proiektuaren izenarekin bat etorri behar da.
namespace JatetxeaAplikazioa.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    // Aplikazioa sortzeko MauiProgram klasea erabiltzen du
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
