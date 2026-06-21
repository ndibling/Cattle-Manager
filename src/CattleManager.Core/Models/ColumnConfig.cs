namespace CattleManager.Core.Models;

public class ColumnConfig
{
    // Default ON — existing columns (user can disable)
    public bool ShowRegisteredName   { get; set; } = true;
    public bool ShowBreed            { get; set; } = true;
    public bool ShowGender           { get; set; } = true;
    public bool ShowAge              { get; set; } = true;
    public bool ShowBirthDate        { get; set; } = true;
    public bool ShowWeight           { get; set; } = true;
    public bool ShowHeight           { get; set; } = true;
    public bool ShowLastWorming      { get; set; } = true;
    public bool ShowStatus           { get; set; } = true;

    // Default OFF — new optional columns
    public bool ShowTagNumber        { get; set; } = false;
    public bool ShowDateAcquired     { get; set; } = false;
    public bool ShowPurchasePrice    { get; set; } = false;
    public bool ShowCurrentValue     { get; set; } = false;
    public bool ShowAskingPrice      { get; set; } = false;
    public bool ShowSalePrice        { get; set; } = false;
    public bool ShowSoldDate         { get; set; } = false;
    public bool ShowBuyerName        { get; set; } = false;
    public bool ShowPastureAddress   { get; set; } = false;
    public bool ShowLastVaccination  { get; set; } = false;
    public bool ShowLastHealthCheck  { get; set; } = false;
    public bool ShowLastHoofTrimming { get; set; } = false;
    public bool ShowSireName         { get; set; } = false;
    public bool ShowDamName          { get; set; } = false;
    public bool ShowExpectedDueDate  { get; set; } = false;
}
