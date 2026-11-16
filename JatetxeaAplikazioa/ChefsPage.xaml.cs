using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using JatetxeaAplikazioa.Models;
using JatetxeaAplikazioa.Services;

namespace JatetxeaAplikazioa.Views;

/// <summary>
/// Sukaldarien kudeaketa-pantaila. Sukaldarien zerrenda bistaratzea, bilaketak egitea, 
/// eta sukaldari berriak sortu, editatu edo ezabatzeko eragiketak kudeatzen ditu App.Db bidez.
/// </summary>
public partial class ChefsPage : ContentPage
{
    // Datu-baseko sukaldari eta errezeta guztiak memorian gordetzeko bildumak
    private List<Chef> allChefs = new();
    private List<RecipeView> allRecipes = new();

    // Une honetan hautatuta dagoen sukaldaria (bat ere hautatuta ez dagoenean null da)
    private Chef? selectedChef;

    // Formularioa edizio-moduan dagoen ala ez adierazteko bandera
    private bool editing = false;

    // Formularioa sukaldari berri bat sortzeko den (true) ala existitzen den bat editatzeko den (false)
    private bool isNew = false;

    /// <summary>
    /// ChefsPage klasearen eraikitzailea. Osagai grafikoak hasieratzen ditu.
    /// </summary>
    public ChefsPage()
    {
        InitializeComponent();
        // Oharra: Aplikazio osoak partekatzen duen App.Db instantzia erabiliko da datu-basearekin komunikatzeko.
    }

    /// <summary>
    /// Pantaila ikusgai jartzen den bakoitzean exekutatzen den metodoa.
    /// Datuak birkargatu eta zain dagoen hautapena (PendingChefId) kudeatzen du.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await KargatuDatuakAsync();

        int? pending = App.PendingChefId;
        App.PendingChefId = null;

        if (pending != null && !string.IsNullOrEmpty(SearchEntry.Text))
            SearchEntry.Text = "";

        ApplyFilter(pending);
    }

    /// <summary>
    /// Datu-basetik sukaldari eta errezeta guztiak asinkronoki kargatzen ditu.
    /// </summary>
    private async Task KargatuDatuakAsync()
    {
        try
        {
            allChefs = await App.Db.GetChefsAsync();
            allRecipes = await App.Db.GetRecipesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errorea", "Ezin izan dira datuak kargatu: " + ex.Message, "Ados");
        }
    }

    /* -------------------------------------------------------------------------
     * BILAKETA ETA IRAGAZPENAK (SEARCH & FILTER)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Bilaketa-kutxako testua aldatzen denean iragazkia aplikatzen du.
    /// </summary>
    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Sartutako testuaren arabera sukaldarien zerrenda iragazten du izena, herrialdea edo espezialitatea kontuan hartuta.
    /// </summary>
    /// <param name="selectId">Iragazi ondoren hautatuta egon behar duen sukaldariaren ID aukerakoa.</param>
    private void ApplyFilter(int? selectId = null)
    {
        string text = (SearchEntry.Text ?? "").Trim().ToLower();
        int? keepId = selectId ?? selectedChef?.Id;

        var list = allChefs.Where(c =>
            string.IsNullOrEmpty(text) ||
            (c.FullName != null && c.FullName.ToLower().Contains(text)) ||
            (c.Country != null && c.Country.ToLower().Contains(text)) ||
            (c.Specialty != null && c.Specialty.ToLower().Contains(text))
        ).ToList();

        ChefList.ItemsSource = list;

        // Sukaldari berria sortzen ari bada, ez da hautapena aldatuko
        if (editing && isNew) return;

        SelectChef(list.FirstOrDefault(c => c.Id == keepId) ?? list.FirstOrDefault());
    }

    /* -------------------------------------------------------------------------
     * HAUTAPENA ETA BISTARATZEA (SELECTION & VIEW)
     * ------------------------------------------------------------------------- */

    /// <summary>
    /// Zerrendan sukaldari bat hautatzen denean gertatzen den ekitaldia.
    /// </summary>
    private void OnChefSelected(object? sender, SelectionChangedEventArgs e)
    {
        ShowChef(e.CurrentSelection.FirstOrDefault() as Chef);
    }

    /// <summary>
    /// Sukaldari bat programatikoki hautatzen du eta bere xehetasunak erakusten ditu.
    /// </summary>
    private void SelectChef(Chef? chef)
    {
        ChefList.SelectedItem = chef;
        ShowChef(chef);
    }

    /// <summary>
    /// Hautatutako sukaldariaren datuak eta bere errezetak eskuineko panelean bistaratzen ditu.
    /// </summary>
    private void ShowChef(Chef? chef)
    {
        selectedChef = chef;
        ViewPanel.BindingContext = chef;

        var recipes = chef != null
            ? allRecipes.Where(r => r.ChefId == chef.Id).OrderBy(r => r.Name).ToList()
            : new List<RecipeView>();

        Chef