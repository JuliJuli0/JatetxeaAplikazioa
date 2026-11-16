using JatetxeaAplikazioa.Models;
using JatetxeProiektua.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace JatetxeaAplikazioa.Views;

/// <summary>
/// Errezeta berriak sortzeko edo existitzen direnak editatzeko formulario-pantaila.
/// Osagaien kudeaketa, balorazioen kontrola eta SQLite datu-basearekiko komunikazioa kudeatzen ditu.
/// </summary>
public partial class RecipeFormPage : ContentPage
{
    // Editatzen ari den errezetaren ID-a (0 bada, errezeta berria sortzen ari da)
    private readonly int recipeId;

    // Datu-basetik kargatutako sukaldarien zerrenda
    private List<Chef> chefs = new();

    // Errezetari lotutako osagaien bilduma dinamikoa
    private readonly ObservableCollection<IngredientLine> lines = new();

    // Une honetan editatzen ari den osagai-lerroa (null bada, berria gehitzen ari da)
    private IngredientLine? editingLine;

    // Errezetaren balorazioa (0.0 eta 10.0 artekoa)
    private double currentRating = 0.0;

    // Kontrol-banderak formularioaren egoera kudeatzeko
    private bool ready = false;
    private bool loading = false;
    private bool changed = false;
    private bool loaded = false;

    /// <summary>
    /// RecipeFormPage klasearen eraikitzailea. Osagaiak hasieratu eta katalogoak kargatzen ditu.
    /// </summary>
    /// <param name="recipeId">Errezeta baten ID-a editatu nahi bada; bestela 0 sortzeko.</param>
    public RecipeFormPage(int recipeId = 0)
    {
        InitializeComponent();
        this.recipeId = recipeId;

        // Leihoaren tamaina aldaketara egokitzeko diseinua
        SizeChanged += (s, e) => MainStack.WidthRequest = Math.Max(320, Math.Min(1100, Width));

        // Aukera-zerrendak kargatu katalogotik
        CuisinePicker.ItemsSource = Catalog.CuisineTypes;
        DifficultyPicker.ItemsSource = Catalog.Difficulties;
        UnitPicker.ItemsSource = Catalog.Units;

        // Osagaien zerrenda lotu UI elementuarekin
        BindableLayout.SetItemsSource(LinesLayout, lines);
        lines.CollectionChanged += (s, e) => CheckForm();

        if (recipeId != 0)
        {
            TitleLabel.Text = "Errezeta editatu";
            SubtitleLabel.Text = "Aldatu errezetaren datuak eta osagaiak";
        }

        ready = true;
    }

    /// <summary>
    /// Pantaila ikusgai jartzen den bakoitzean exekutatzen da. 
    /// Sukaldarien zerrenda eguneratzen du datu-basetik.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // OSO GARRANTZITSUA: Picker-aren iturria null ezartzen da MAUI-k 
            // aldaketa antzeman dezan eta zerrenda berria osorik marraz dezan.
            ChefPicker.ItemsSource = null;

            chefs = await App.Db.GetChefsAsync();
            ChefPicker.ItemsSource = chefs;

            if (!loaded)
            {
                if (recipeId != 0)
                {
                    var all = await App.Db.GetRecipesAsync();
                    var recipe = all.FirstOrDefault(r => r.Id == recipeId);
                    if (recipe != null) FillForm(recipe);
                }
                loaded = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errorea", "Ezin izan dira datuak kargatu: " + ex.Message, "Ados");
        }

        CheckForm();
        changed = false;
    }

    /// <summary>
    /// Existitzen den errezeta baten datuekin formularioa betetzen du.
    /// </summary>
    private void FillForm(RecipeView recipe)
    {
        loading = true;

        NameEntry.Text = recipe.Name;
        ChefPicker.SelectedItem = chefs.FirstOrDefault(c => c.Id == recipe.ChefId);
        CuisinePicker.SelectedItem = recipe.CuisineType;
        DifficultyPicker.SelectedItem = recipe.Difficulty;
        TimeEntry.Text = recipe.PrepMinutes.ToString();

        currentRating = recipe.Rating;
        UpdateRatingDisplay();

        InstructionsEditor.Text = recipe.Instructions;

        lines.Clear();
        foreach (var l in recipe.Ingredients)
        {
            lines.Add(new IngredientLine
            {
                IngredientId = l.IngredientId,
                Name = l.Name,
                Quantity = l.Quantity,
                Unit = l.Unit
            });
        }

        loading = false;
        changed = false;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e) => CheckForm();
    private void OnPickerChanged(object? sender, EventArgs e) => CheckForm();

    /* -------------------------------------------------------------------------
     * BALORAZIOAREN KUDEAKETA (RATING MANAGEMENT)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Balorazioa 0.5 puntu murrizten du.
    /// </summary>
    private void OnDecreaseRatingClicked(object? sender, EventArgs e)
    {
        if (currentRating > 0.0)
        {
            currentRating = Math.Max(0.0, currentRating - 0.5);
            UpdateRatingDisplay();
        }
    }

    /// <summary>
    /// Balorazioa 0.5 puntu handitzen du.
    /// </summary>
    private void OnIncreaseRatingClicked(object? sender, EventArgs e)
    {
        if (currentRating < 10.0)
        {
            currentRating = Math.Min(10.0, currentRating + 0.5);
            UpdateRatingDisplay();
        }
    }

    /// <summary>
    /// Balorazioaren balioa mugatu eta pantailan modu egokian bistaratzen du.
    /// </summary>
    private void UpdateRatingDisplay()
    {
        currentRating = Math.Clamp(currentRating, 0.0, 10.0);
        RatingLabel.Text = $"{currentRating.ToString("0.1", CultureInfo.InvariantCulture)} / 10";
        CheckForm();
    }

    /// <summary>
    /// Prestaketa-denboraren baliozkotzea egiaztatzen du (1 eta 600 minutu artekoa).
    /// </summary>
    private bool TryGetMinutes(out int minutes)
    {
        bool ok = int.TryParse(TimeEntry.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out minutes);
        return ok && minutes >= Catalog.MinPrepMinutes && minutes <= Catalog.MaxPrepMinutes;
    }

    /// <summary>
    /// Formularioaren eremu guztiak beteta eta baliozkoak dauden egiaztatzen du gorde ahal izateko.
    /// </summary>
    private void CheckForm()
    {
        if (!ready) return;
        if (!loading) changed = true;

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(NameEntry.Text)) missing.Add("Izena");
        if (ChefPicker.SelectedItem == null) missing.Add("Sukaldaria");
        if (CuisinePicker.SelectedItem == null) missing.Add("Sukaldaritza mota");
        if (DifficultyPicker.SelectedItem == null) missing.Add("Zailtasuna");

        bool timeOk = TryGetMinutes(out _);
        if (!timeOk) missing.Add("Prestaketa-denbora (1-600)");
        TimeErrorLabel.IsVisible = !timeOk;

        if (lines.Count == 0) missing.Add("gutxienez osagai bat");
        NoLinesLabel.IsVisible = lines.Count == 0;

        MissingLabel.Text = missing.Count == 0 ? "" : "Falta da: " + string.Join(", ", missing);
        MissingLabel.IsVisible = missing.Count > 0;
        SaveButton.IsEnabled = missing.Count == 0;
    }

    /* -------------------------------------------------------------------------
     * OSAGAIEN KUDEAKETA (INGREDIENT MANAGEMENT)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Osagai berri bat gehitu edo existitzen dena eguneratzen du zerrendan.
    /// </summary>
    private async void OnAddLineClicked(object? sender, EventArgs e)
    {
        LineErrorLabel.IsVisible = false;

        string ingredientName = (IngredientEntry.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ingredientName))
        {
            ShowLineError("Idatzi osagai baten izena.");
            return;
        }

        string text = (QuantityEntry.Text ?? "").Trim().Replace(',', '.');
        if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double quantity)
            || quantity <= 0 || quantity > 100000)
        {
            ShowLineError("Sartu kantitate baliozko bat (0 baino handiagoa).");
            return;
        }

        var unit = UnitPicker.SelectedItem as string;
        if (unit == null)
        {
            ShowLineError("Aukeratu unitatea.");
            return;
        }

        try
        {
            int ingredientId = await App.Db.GetOrCreateIngredientAsync(ingredientName);

            bool duplicated = lines.Any(l => l.IngredientId == ingredientId && l != editingLine);
            if (duplicated)
            {
                ShowLineError($"\"{ingredientName}\" dagoeneko gehituta dago errezeta honetan.");
                return;
            }

            var line = new IngredientLine
            {
                IngredientId = ingredientId,
                Name = ingredientName,
                Quantity = quantity,
                Unit = unit
            };

            if (editingLine != null)
            {
                int index = lines.IndexOf(editingLine);
                if (index >= 0) lines[index] = line;
                else lines.Add(line);
            }
            else
            {
                lines.Add(line);
            }

            ResetLineEditor();
        }
        catch (Exception ex)
        {
            ShowLineError("Errorea osagaia gordetzean: " + ex.Message);
        }
    }

    /// <summary>
    /// Aukeratutako osagai-lerroa editatzeko moduan jartzen du.
    /// </summary>
    private void OnEditLineClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not IngredientLine line) return;

        editingLine = line;
        IngredientEntry.Text = line.Name;
        QuantityEntry.Text = line.QuantityText;
        UnitPicker.SelectedItem = line.Unit;

        LineErrorLabel.IsVisible = false;
        AddLineButton.Text = "Eguneratu osagaia";
        CancelLineButton.IsVisible = true;
    }

    /// <summary>
    /// Osagai-lerro bat zerrendatik kentzen du.
    /// </summary>
    private void OnRemoveLineClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not IngredientLine line) return;

        if (line == editingLine) ResetLineEditor();
        lines.Remove(line);
    }

    private void OnCancelLineClicked(object? sender, EventArgs e) => ResetLineEditor();

    /// <summary>
    /// Osagaiak sartzeko eremuak eta egoera hasierako balioetara ekartzen ditu.
    /// </summary>
    private void ResetLineEditor()
    {
        editingLine = null;
        IngredientEntry.Text = "";
        QuantityEntry.Text = "";
        UnitPicker.SelectedItem = null;
        LineErrorLabel.IsVisible = false;
        AddLineButton.Text = "Gehitu osagaia";
        CancelLineButton.IsVisible = false;
    }

    private void ShowLineError(string message)
    {
        LineErrorLabel.Text = message;
        LineErrorLabel.IsVisible = true;
    }

    /* -------------------------------------------------------------------------
     * GORDE ETA UTZI (SAVE & CANCEL)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Errezetaren datuak eta osagaiak datu-basetik gordetzen ditu (berria edo eguneratzea).
    /// </summary>
    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!TryGetMinutes(out int minutes)) return;

        if (ChefPicker.SelectedItem is not Chef chef ||
            CuisinePicker.SelectedItem is not string cuisine ||
            DifficultyPicker.SelectedItem is not string difficulty ||
            lines.Count == 0) return;

        var recipe = new Recipe
        {
            Id = recipeId,
            Name = NameEntry.Text!.Trim(),
            ChefId = chef.Id,
            CuisineType = cuisine,
            Difficulty = difficulty,
            PrepMinutes = minutes,
            Rating = currentRating,
            Instructions = (InstructionsEditor.Text ?? "").Trim()
        };

        var links = lines.Select(l => new RecipeIngredient
        {
            IngredientId = l.IngredientId,
            Quantity = l.Quantity,
            Unit = l.Unit
        }).ToList();

        try
        {
            int savedId;
            if (recipeId == 0)
            {
                savedId = await App.Db.AddRecipeAsync(recipe, links);
            }
            else
            {
                await App.Db.UpdateRecipeAsync(recipe, links);
                savedId = recipeId;
            }

            App.PendingRecipeId = savedId;
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errorea", "Ezin izan da errezeta gorde: " + ex.Message, "Ados");
        }
    }

    /// <summary>
    /// Aldaketak bertan behera utzi eta aurreko pantailara bueltatzen da (aldaketak baldin badaude abisatu ostean).
    /// </summary>
    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (changed)
        {
            bool leave = await DisplayAlert("Utzi", "Sartutako datuak galduko dira. Irten nahi duzu?", "Bai", "Ez");
            if (!leave) return;
        }

        await Navigation.PopAsync();
    }
}