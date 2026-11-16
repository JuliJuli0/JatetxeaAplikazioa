namespace JatetxeaAplikazioa.Models;

/// <summary>
/// Aplikazioan erabiltzen diren balio eta konstante fikoak.
/// </summary>
public static class Catalog
{
    // Prestaketa-denboraren mugak minututan
    public const int MinPrepMinutes = 1;
    public const int MaxPrepMinutes = 600;
    public const int TimeStep = 30;

    // Bilaketa irizpideak aukeratzeko konstanteak
    public const string CriterionAll = "Denak";
    public const string CriterionName = "Izena";
    public const string CriterionChef = "Sukaldaria";
    public const string CriterionIngredient = "Osagaia";      
    public const string CriterionCuisine = "Sukaldaritza mota";

    public static readonly string[] SearchCriteria =
    {
        CriterionAll, CriterionName, CriterionChef, CriterionIngredient, CriterionCuisine 
    };

    // Erabilgarri dauden zailtasun mailak
    public static readonly string[] Difficulties = { "Erraza", "Ertaina", "Zaila" };

    // Sukaldaritza mota desberdinak zerrenda eta iragazkietarako
    public static readonly string[] CuisineTypes =
    {
        "Euskal sukaldaritza", "Espainiarra", "Italiarra", "Frantsesa",
        "Mediterraneoa", "Japoniarra", "Mexikarra", "Indiarra"
    };

    // Neurri-unitateak osagaietarako
    public static readonly string[] Units =
    {
        "g", "kg", "ml", "l", "koilarakada", "kilarakada txikia", "pitin bat", "ale", "tamaina desberdinekoak"
    };
}