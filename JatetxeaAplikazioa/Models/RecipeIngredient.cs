using SQLite;

namespace JatetxeProiektua.Models;

/// <summary>
/// Errezeta eta osagaien arteko lotura taula (kantitatea eta unitatea gordetzeko).
/// Oharra: (RecipeId, IngredientId) bikotea bakarra da; beraz, osagai bera 
/// ezin da bi aldiz errepikatu errezeta berean.
/// </summary>
public class RecipeIngredient
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Lotutako errezetaren ID-a (Kanpoko gakoa).</summary>
    [Indexed("UX_RecipeIngredient", 1, Unique = true)]
    public int RecipeId { get; set; }

    /// <summary>Lotutako osagaiaren ID-a (Kanpoko gakoa).</summary>
    [Indexed("UX_RecipeIngredient", 2, Unique = true)]
    public int IngredientId { get; set; }

    /// <summary>Erabilitako osagai-kantitatea.</summary>
    public double Quantity { get; set; }

    /// <summary>Neurri-unitatea (adib. "g", "kg", "ml", "unitatea", etab.).</summary>
    public string Unit { get; set; } = "";
}