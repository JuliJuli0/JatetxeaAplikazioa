namespace JatetxeaAplikazioa;

public static class MauiProgram
{
    // Aplikazioa sortzen duen metodoa (Windows-en hasierako puntua)
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}
