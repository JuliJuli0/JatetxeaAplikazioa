using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using JatetxeaAplikazioa.Models;

namespace JatetxeaAplikazioa.Views;

/// <summary>
/// Errezeten pantaila nagusiaren kudeaketa. Errezeten zerrenda, testu-bilaketak, 
/// iragazkiak (denbora eta irizpideak) eta xehetasunen bistaratzea kudeatzen ditu.
/// </summary>
public partial class RecipesPage : ContentPage
{
    // Datu-baseko errezeta guztiak memorian gordetzeko bilduma (iragazi gabe)
    private List<RecipeView> allRecipes = new();

    // Une honetan aukeratuta dagoen errezeta (bat ere hautatuta ez dagoenean null da)
    private RecipeView? selected;

    // Kontrolak hasieratzen ari diren ala ez adierazteko bandera (gertaerak desegoki ez exekutatzeko)
    private bool ready = false;

    /// <summary>
    /// RecipesPage klasearen eraikitzailea. Osagaiak hasieratu eta bilaketa-irizpideak kargatzen ditu.
    /// </summary>
    public RecipesPage()
    {
        InitializeComponent();

        // Bilaketa-irizpideak Picker elementuan ezarri
        CriterionPicker.ItemsSource = Catalog.SearchCriteria;
        CriterionPicker.SelectedIndex = 0; // Lehenetsia: "Denak"

        ready = true;
    }

    /// <summary>
    /// Pantaila ikusgai jartzen den bakoitzean datuak datu-basetik berritzen dira.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            allRecipes = await App.Db.GetRecipesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errorea", "Ezin izan dira errezetak kargatu: " + ex.Message, "Ados");
            return;
        }

        // Beste pantaila batetik errezeta zehatz bat ikusteko eskatu bada (PendingRecipeId bidez)
        int? pending = App.PendingRecipeId;
        App.PendingRecipeId = null;

        if (pending != null)
        {
            ready = false;
            SearchEntry.Text = "";
            CriterionPicker.SelectedIndex = 0;
            TimeSlider.Value = 20;
            TimeLabel.Text = "Mugarik gabe";
            ready = true;
        }

        ApplyFilter(pending);
    }

    /* -------------------------------------------------------------------------
     * IRAGAZKIAK ETA BILAKETA (FILTERS & SEARCH)
     * ------------------------------------------------------------------------- */

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (!ready) return;
        ApplyFilter();
    }

    private void OnCriterionChanged(object? sender, EventArgs e)
    {
        if (!ready) return;
        ApplyFilter();
    }

    /// <summary>
    /// Denbora-sliderra mugitzean balioa borobiltzen du eta dagokion testu-etiketa eguneratzen du.
    /// </summary>
    private void OnTimeSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (!ready) return;

        double rounded = Math.Round(e.NewValue);
        if (rounded != TimeSlider.Value)
        {
            TimeSlider.Value = rounded;
            return;
        }

        int minutes = (int)rounded * Catalog.TimeStep;

        if (minutes >= Catalog.MaxPrepMinutes)
            TimeLabel.Text = "Mugarik gabe";
        else
            TimeLabel.Text = $"Gehienez {minutes} min";

        ApplyFilter();
    }

    /// <summary>
    /// Bilaketa-testuaren, irizpidearen eta prestaketa-denboraren arabera errezeten zerrenda iragazten du.
    /// </summary>
    /// <param name="selectId">Iragazi eta gero hautatuta geratu behar den errezetaren ID aukerakoa.</param>
    private void ApplyFilter(int? selectId = null)
    {
        string text = (SearchEntry.Text ?? "").Trim();
        int maxMinutes = (int)TimeSlider.Value * Catalog.TimeStep;

        int? keepId = selectId ?? selected?.Id;

        var list = new List<RecipeView>();
        foreach (var r in allRecipes)
        {
            // Denbora-iragazkia aplikatu
            if (maxMinutes < Catalog.MaxPrepMinutes && r.PrepMinutes > maxMinutes)
                continue;

            // Testu-bilaketa aplikatu
            if (text.Length > 0 && !Matches(r, text))
                continue;

            list.Add(r);
        }

        RecipeList.ItemsSource = list;

        // Hautapena mantendu edo zerrendako lehenengoa aukeratu
        RecipeView? toSelect = list.FirstOrDefault(r => r.Id == keepId) ?? list.FirstOrDefault();
        RecipeList.SelectedItem = toSelect;
        ShowSelected(toSelect);
    }

    /// <summary>
    /// Errezeta batek aukeratutako irizpidearen arabera bilaketa-baldintzak betetzen dituen egiaztatzen du.
    /// </summary>
    private bool Matches(RecipeView r, string text)
    {
        string criterion = CriterionPicker.SelectedItem as string ?? Catalog.CriterionAll;

        bool name = Contains(r.Name, text);
        bool chef = Contains(r.ChefName, text);
        bool cuisine = Contains(r.CuisineType, text);
        bool ingredient = r.Ingredients.Any(i => Contains(i.Name, text));

        return criterion switch
        {
            Catalog.CriterionName => name,
            Catalog.CriterionChef => chef,
            Catalog.CriterionIngredient => ingredient,
            Catalog.CriterionCuisine => cuisine,
            _ => name || chef || cuisine || ingredient, // "Denak" kasu lehenetsia
        };
    }

    /// <summary>
    /// Testu-kate batek beste bat barne hartzen duen egiaztatzen du (maiuskula/minuskulak bereiztu gabe).
    /// </summary>
    private static bool Contains(string? source, string text)
    {
        return source != null && source.Contains(text, StringComparison.CurrentCultureIgnoreCase);
    }

    /* -------------------------------------------------------------------------
     * HAUTAPENA ETA BISTARATZEA (SELECTION & VIEW)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Zerrendan errezeta bat hautatzen denean xehetasunak erakusteko deitzen da.
    /// </summary>
    private void OnRecipeSelected(object? sender, SelectionChangedEventArgs e)
    {
        ShowSelected(e.CurrentSelection.FirstOrDefault() as RecipeView);
    }

    /// <summary>
    /// Hautatutako errezetaren xehetasunak eskuineko panelean bistaratzen ditu.
    /// </summary>
    private void ShowSelected(RecipeView? recipe)
    {
        selected = recipe;

        DetailsPanel.BindingContext = recipe;
        DetailsPanel.IsVisible = recipe != null;
        EmptyLabel.IsVisible = recipe == null;

        // Editatu eta ezabatu botoiak errezeta bat hautatuta dagoenean bakarrik gaitzen dira
        EditButton.IsEnabled = recipe != null;
        DeleteButton.IsEnabled = recipe != null;
    }

    /* -------------------------------------------------------------------------
     * BOTOIEN EKINTZAK (ACTIONS)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Errezeta berria sortzeko formulario-pantailara bideratzen du.
    /// </summary>
    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RecipeFormPage());
    }

    /// <summary>
    /// Hautatutako errezeta editatzeko formulario-pantailara bideratzen du.
    /// </summary>
    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (selected == null) return;
        await Navigation.PushAsync(new RecipeFormPage(selected.Id));
    }

    /// <summary>
    /// Hautatutako errezetaren sukaldariaren pantailara salto egiten du.
    /// </summary>
    private async void OnViewChefClicked(object? sender, EventArgs e)
    {
        if (selected == null) return;

        App.PendingChefId = selected.ChefId;
        await Shell.Current.GoToAsync("//ChefsPage");
    }

    /// <summary>
    /// Hautatutako errezeta datu-basetik ezabatzen du konfirmazioa eskatu ostean.
    /// </summary>
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (selected == null) return;

        bool ok = await DisplayAlert("Errezeta ezabatu",
            $"\"{selected.Name}\" errezeta ezabatu nahi duzu? Ekintza hau ezin da desegin.",
            "Bai", "Ez");
        if (!ok) return;

        try
        {
            await App.Db.DeleteRecipeAsync(selected.Id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errorea", "Ezin izan da ezabatu: " + ex.Message, "Ados");
            return;
        }

        selected = null;
        allRecipes = await App.Db.GetRecipesAsync();
        ApplyFilter();
    }
}