namespace DocWatcher.Core.Models;

public class Document
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataScadenza { get; set; }
    public string? PercorsoAllegato { get; set; }
}
