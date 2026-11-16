namespace JatetxeaAplikazioa.Models;

/// <summary>
/// Errezetako osagai-lerro bakoitza pantailan erakusteko modelo laguntzailea.
/// (Osagaiaren izena, kantitatea eta unitatea biltzen ditu).
/// </summary>
public class IngredientLine
{
    public int IngredientId { get; set; }

    /// <summary>Osagaiaren izena (adib. "Patata").</summary>
    public string Name { get; set; } = "";

    /// <summary>Erabilitako kantitatea.</summary>
    public double Quantity { get; set; }

    /// <summary>Neurri-unitatea (adib. "g", "ml", "unitatea").</summary>
    public string Unit { get; set; } = "";

    /// <summary>Kantitatea testu formatuan bihurtzen du (adib. 2 edo 0,5).</summary>
    public string QuantityText => Quantity.ToString("0.##");

    /// <summary>Osagai osoaren testu deskriptiboa (adibidez: "400 g").</summary>
    public string Display => $"{QuantityText} {Unit}";
}


/// <summary>
/// Errezeta bat bere sukaldariaren izenarekin eta osagaien zerrendarekin 
/// zuzenean pantailan (UI) erakusteko prest prestatutako bista-modelo konbinatua.
/// </summary>
public class RecipeView
{
    public int Id { get; set; }

    /// <summary>Errezetaren izenburua.</summary>
    public string Name { get; set; } = "";

    /// <summary>Sukaldaritza mota (adib. Euskal sukaldaritza).</summary>
    public string CuisineType { get; set; } = "";

    /// <summary>Zailtasun maila (Erraza, Ertaina, Zaila).</summary>
    public string Difficulty { get; set; } = "";

    /// <summary>Prestaketa denbora minututan.</summary>
    public int PrepMinutes { get; set; }

    /// <summary>Errezetaren balorazioa (0 - 10).</summary>
    public double Rating { get; set; }

    /// <summary>Egiteko urratsak edo azalpenak.</summary>
    public string Instructions { get; set; } = "";

    public int ChefId { get; set; }

    /// <summary>Errezeta sortu duen sukaldariaren izen-abizen osoak.</summary>
    public string ChefName { get; set; } = "";

    /// <summary>Errezetak dituen osagaien lerroen zerrenda.</summary>
    public List<IngredientLine> Ingredients { get; set; } = new();

    // --- Pantailan (UI) zuzenean lotura egiteko propietate laguntzaileak ---

    /// <summary>Denbora testu formatoan (adib. "45 min").</summary>
    public string PrepText => $"{PrepMinutes} min";

    /// <summary>Balorazioa ikono eta guzti (adib. "★ 9,5 / 10").</summary>
    public string RatingText => $"★ {Rating:0.0} / 10";

    /// <summary>Azpititulua elementuak zerrendetan erakusteko.</summary>
    public string Subtitle => $"{ChefName}  ·  {PrepMinutes} min  ·  ★ {Rating:0.0}";

    /// <summary>Argibiderik badagoen egiaztatzeko balio du.</summary>
    public bool HasInstructions => !string.IsNullOrWhiteSpace(Instructions);
}