using System;

namespace DocWatcher.Wpf.DTO;

public class DocumentInput
{
	public string? Titolo { get; set; }
	public DateTime? DataScadenza { get; set; }
	public string? PercorsoAllegato { get; set; }
}
