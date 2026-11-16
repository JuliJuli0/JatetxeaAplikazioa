using SQLite;

namespace JatetxeaAplikazioa.Models;

/// <summary>
/// Sukaldariaren datuak gordetzeko modeloa (SQLite taula).
/// </summary>
public class Chef
{
    // TODO: Ziurtatu eremu guztiak betetzen direla erregistroa gorde aurretik
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Sukaldariaren izena.</summary>
    public string FirstName { get; set; } = "";

    /// <summary>Sukaldariaren abizena.</summary>
    public string LastName { get; set; } = "";

    /// <summary>Jatorrizko herrialdea.</summary>
    public string Country { get; set; } = "";

    /// <summary>Espezialitate nagusia.</summary>
    public string Specialty { get; set; } = "";

    /* Izena eta abizena elkartuta itzultzen dituen propietatea.
       Ez da datu-basean gordetzen ([Ignore] atributuari esker). */
    [Ignore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}