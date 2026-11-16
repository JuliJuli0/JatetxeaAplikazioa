using SQLite;

namespace JatetxeaAplikazioa.Models;

/// <summary>
/// Errezeten datuak gordetzeko modeloa (SQLite taula).
/// </summary>
public class Recipe
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Errezetaren izenburua.</summary>
    public string Name { get; set; } = "";

    /// <summary>Sukaldaritza mota (adib. Euskal sukaldaritza).</summary>
    public string CuisineType { get; set; } = "";

    /// <summary>Zailtasun maila (Erraza, Ertaina, Zaila).</summary>
    public string Difficulty { get; set; } = "";

    /// <summary>Prestaketa denbora minututan.</summary>
    public int PrepMinutes { get; set; }

    /// <summary>Errezetaren balorazioa (0-10 artean).</summary>
    public double Rating { get; set; }

    /// <summary>Egiteko azalpenak eta pausoak.</summary>
    public string Instructions { get; set; } = "";

    // NOTE: Kanpoko gakoa (Foreign Key) Sukaldariarekin lotzeko
    [Indexed]
    public int ChefId { get; set; }
}