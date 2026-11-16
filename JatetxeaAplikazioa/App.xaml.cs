using JatetxeaAplikazioa.Services;

namespace JatetxeaAplikazioa;

/// <summary>
/// Aplikazioaren klase nagusia.
/// </summary>
public partial class App : Application
{
    // Datu-basea aplikazio osoan erabili ahal izateko aldagaia
    public static RecipeDatabase Db { get; private set; } = null!;

    // Pantailen arteko nabigazioan IDak pasatzeko egoera-aldagaiak
    public static int? PendingChefId { get; set; }
    public static int? PendingRecipeId { get; set; }

    public App()
    {
        InitializeComponent();

        // Aplikazioak beti tema argia erabiltzea behartzen da
        UserAppTheme = AppTheme.Light;

        // Datu-base lokala sortu eta inizializatu
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "errezetak.db3");
        Db = new RecipeDatabase(dbPath);
    }

    /// <summary>
    /// Aplikazioaren leihoa sortu eta bere hasierako neurriak finkatzen ditu.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "Errezeten Kudeaketa",
            Width = 1200,
            Height = 780,
            MinimumWidth = 960,
            MinimumHeight = 620
        };
    }
}