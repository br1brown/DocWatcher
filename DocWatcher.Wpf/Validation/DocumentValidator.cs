using System.Collections.Generic;
using DocWatcher.Wpf.DTO;

namespace DocWatcher.Wpf.Validation;

public static class DocumentValidator
{
	public static List<string> Validate(DocumentInput input)
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(input.Titolo))
			errors.Add("Il titolo è obbligatorio.");

		if (input.DataScadenza is null)
			errors.Add("La data di scadenza è obbligatoria.");

		// In futuro puoi aggiungere altre regole qui:
		// - Data non nel passato
		// - Percorso allegato valido, ecc.

		return errors;
	}
}
