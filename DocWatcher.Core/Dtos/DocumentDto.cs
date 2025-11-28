using System;
using System.Collections.Generic;

namespace DocWatcher.Core.Dtos;

public class DocumentDto
{
    public int? Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataScadenza { get; set; } = DateTime.MinValue;
    public string? PercorsoAllegato { get; set; }
}
