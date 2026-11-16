using SQLite;

namespace JatetxeProiektua.Models;

/// <summary>
/// Osagaien katalogoa gordetzeko modeloa (SQLite taula).
/// Errezeta bati osagarri bat gehitzean, zerrenda honetatik hautatzen da.
/// </summary>
public class Ingredient
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Osagaiaren izena. 
    /// Oharra: [Unique] atributuari esker, ezin da izen bera bi aldiz erregistratu.
    /// </summary>
    [Unique]
    public string Name { get; set; } = "";
}