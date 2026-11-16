using JatetxeaAplikazioa.Models;
using JatetxeProiektua.Models;
using SQLite;

namespace JatetxeaAplikazioa.Services;

/// <summary>
/// Errezeten datu-base lokalarekin (SQLite) komunikatzeko zerbitzu nagusia.
/// Taulak sortzea, hasierako datuak kargatzea eta CRUD eragiketak kudeatzen ditu.
/// </summary>
public class RecipeDatabase
{
    // Datu-base fitxategiaren kokapen-bidea gailuan
    private readonly string _path;

    // SQLite konexio asinkronoaren instantzia
    private SQLiteAsyncConnection? _db;

    // Hari anitzeko (multi-thread) kontstantean arazoak saihesteko semaforoa
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// RecipeDatabase klasearen eraikitzailea.
    /// </summary>
    /// <param name="path">SQLite fitxategiaren bide absolutua.</param>
    public RecipeDatabase(string path) => _path = path;

    /// <summary>
    /// Datu-basearekiko konexio asinkronoa lortzen du. 
    /// Lehen aldiz irekitzean, taulak sortzen ditu eta, hutsik badago, datu-lehenetsiak (Seed) txertatzen ditu.
    /// </summary>
    /// <returns>SQLite konexio asinkrono aktiboa.</returns>
    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db != null) return _db;

        // Lehia-egoerak (race conditions) ekiditeko semaforoa blokeatzen da
        await _lock.WaitAsync();
        try
        {
            if (_db != null) return _db;

            // Konexioa sortu eta dagozkion taulak definitu
            _db = new SQLiteAsyncConnection(_path);
            await _db.CreateTableAsync<Chef>();
            await _db.CreateTableAsync<Ingredient>();
            await _db.CreateTableAsync<Recipe>();
            await _db.CreateTableAsync<RecipeIngredient>();

            // Datu-basea erabat hutsik badago, adibidezko datu basea (Seed) kargatzen da
            if (await _db.Table<Chef>().CountAsync() == 0)
            {
                await _db.RunInTransactionAsync(Seed);
            }
            return _db;
        }
        finally
        {
            // Blokeoa askatzen da beste prozesuek erabili ahal izateko
            _lock.Release();
        }
    }

    /* -------------------------------------------------------------------------
     * SUKALDARIEN KUDEAKETA (CHEF OPERATIONS)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Sukaldari guztien zerrenda eskuratzen du, izen osoaren arabera alfabetikoki ordenatuta.
    /// </summary>
    public async Task<List<Chef>> GetChefsAsync() =>
        (await (await GetDbAsync()).Table<Chef>().ToListAsync()).OrderBy(c => c.FullName).ToList();

    /// <summary>
    /// Sukaldari berri bat gordetzen du (ID-a 0 bada) edo existitzen den bat eguneratzen du.
    /// </summary>
    public async Task<int> SaveChefAsync(Chef chef) =>
        chef.Id == 0 ? await (await GetDbAsync()).InsertAsync(chef) : await (await GetDbAsync()).UpdateAsync(chef);

    /// <summary>
    /// Sukaldari bat eta hari lotutako errezeta eta erlazio guztiak ezabatzen ditu transakzio batean.
    /// </summary>
    public async Task DeleteChefAsync(int chefId)
    {
        var db = await GetDbAsync();
        await db.RunInTransactionAsync(conn =>
        {
            // Sukaldari honen errezeta guztiak eta haien osagai-loturak garbitu
            foreach (var r in conn.Table<Recipe>().Where(r => r.ChefId == chefId).ToList())
            {
                conn.Execute("DELETE FROM RecipeIngredient WHERE RecipeId = ?", r.Id);
                conn.Execute("DELETE FROM Recipe WHERE Id = ?", r.Id);
            }
            // Azkenik, sukaldaria ezabatu
            conn.Execute("DELETE FROM Chef WHERE Id = ?", chefId);
        });
    }

    /* -------------------------------------------------------------------------
     * OSAGAIEN KUDEAKETA (INGREDIENT OPERATIONS)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Katalogo osoan dauden osagaiak eskuratzen ditu, izenez ordenatuta.
    /// </summary>
    public async Task<List<Ingredient>> GetIngredientsAsync() =>
        (await (await GetDbAsync()).Table<Ingredient>().ToListAsync()).OrderBy(i => i.Name).ToList();

    /// <summary>
    /// Izenaren arabera osagai bat bilatzen du (maiuskula/minuskulak bereiztu gabe). 
    /// Ez bada existitzen, sortu eta datu-basean gordetzen du automatikoki.
    /// </summary>
    public async Task<int> GetOrCreateIngredientAsync(string name)
    {
        var db = await GetDbAsync();
        name = name.Trim();
        var all = await db.Table<Ingredient>().ToListAsync();

        var existing = all.FirstOrDefault(i =>
            string.Equals(i.Name, name, StringComparison.CurrentCultureIgnoreCase));
        if (existing != null) return existing.Id;

        var ingredient = new Ingredient { Name = name };
        await db.InsertAsync(ingredient);
        return ingredient.Id;
    }

    /* -------------------------------------------------------------------------
     * ERREZETEN KUDEAKETA (RECIPE OPERATIONS)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Errezeta guztiak eskuratzen ditu, bakoitzaren sukaldaria eta osagai-lerroak lotuz (RecipeView formatuan).
    /// </summary>
    public async Task<List<RecipeView>> GetRecipesAsync()
    {
        var db = await GetDbAsync();
        var recipes = await db.Table<Recipe>().ToListAsync();
        var chefs = await db.Table<Chef>().ToListAsync();
        var ingredients = await db.Table<Ingredient>().ToListAsync();
        var links = await db.Table<RecipeIngredient>().ToListAsync();

        return recipes.Select(r => new RecipeView
        {
            Id = r.Id,
            Name = r.Name,
            CuisineType = r.CuisineType,
            Difficulty = r.Difficulty,
            PrepMinutes = r.PrepMinutes,
            Rating = r.Rating,
            Instructions = r.Instructions,
            ChefId = r.ChefId,
            ChefName = chefs.FirstOrDefault(c => c.Id == r.ChefId)?.FullName ?? "?",
            Ingredients = links.Where(l => l.RecipeId == r.Id)
                .Select(l => new IngredientLine
                {
                    IngredientId = l.IngredientId,
                    Name = ingredients.FirstOrDefault(i => i.Id == l.IngredientId)?.Name ?? "?",
                    Quantity = l.Quantity,
                    Unit = l.Unit
                }).OrderBy(x => x.Name).ToList()
        }).OrderBy(r => r.Name).ToList();
    }

    /// <summary>
    /// Errezeta berri bat eta bere osagai-zerrenda datu-basean txertatzen ditu transakzio baten bidez.
    /// </summary>
    public async Task<int> AddRecipeAsync(Recipe recipe, List<RecipeIngredient> lines)
    {
        var db = await GetDbAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Insert(recipe);
            foreach (var l in lines)
            {
                l.RecipeId = recipe.Id;
                conn.Insert(l);
            }
        });
        return recipe.Id;
    }

    /// <summary>
    /// Existitzen den errezeta bat eta bere osagai-loturak eguneratzen ditu.
    /// </summary>
    public async Task UpdateRecipeAsync(Recipe recipe, List<RecipeIngredient> lines)
    {
        var db = await GetDbAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Update(recipe);
            // Aurreko erlazioak ezabatu eta berriak txertatzen dira aldaketak egokiro jasotzeko
            conn.Execute("DELETE FROM RecipeIngredient WHERE RecipeId = ?", recipe.Id);
            foreach (var l in lines)
            {
                l.Id = 0;
                l.RecipeId = recipe.Id;
                conn.Insert(l);
            }
        });
    }

    /// <summary>
    /// Errezeta bat eta bere osagai-loturak erabat ezabatzen ditu datu-basetik.
    /// </summary>
    public async Task DeleteRecipeAsync(int recipeId)
    {
        var db = await GetDbAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Execute("DELETE FROM RecipeIngredient WHERE RecipeId = ?", recipeId);
            conn.Execute("DELETE FROM Recipe WHERE Id = ?", recipeId);
        });
    }

    /* -------------------------------------------------------------------------
     * HASIERAKO DATUAK (SEED DATA)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Aplikazioa lehen aldiz abiaraztean kargatzen den adibide-datu multzoa (Seed).
    /// </summary>
    private static void Seed(SQLiteConnection conn)
    {
        // Oinarrizko osagaiak sortu
        var patata = new Ingredient { Name = "Patata" };
        conn.Insert(patata);

        var hegaluzea = new Ingredient { Name = "Hegaluzea" };
        conn.Insert(hegaluzea);

        // Sukaldari bat sortu
        var chef = new Chef
        {
            FirstName = "Aitor",
            LastName = "Egiguren Uranga",
            Country = "Euskal Herria",
            Specialty = "Tradizionala"
        };
        conn.Insert(chef);

        // Errezeta bat sortu eta sukaldariari lotu
        var recipe = new Recipe
        {
            Name = "Marmitakoa",
            CuisineType = "Euskal sukaldaritza",
            Difficulty = "Ertaina",
            PrepMinutes = 45,
            Rating = 9.5,
            ChefId = chef.Id,
            Instructions = "1. Prestatu sofritoa tipula, piper eta tomatearekin.\n2. Gehitu patatak ukitu batean moztuta eta arrain-salda.\n3. Egosi 25 minutuz eta gehitu hegaluze zatiak amaieran."
        };
        conn.Insert(recipe);

        // Errezetaren osagaien erlazioak ezarri kantitate eta unitateekin
        conn.Insert(new RecipeIngredient { RecipeId = recipe.Id, IngredientId = patata.Id, Quantity = 800, Unit = "g" });
        conn.Insert(new RecipeIngredient { RecipeId = recipe.Id, IngredientId = hegaluzea.Id, Quantity = 400, Unit = "g" });
    }
}