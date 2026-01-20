using System.Collections.Generic;
using DocWatcher.Wpf.DTO;

namespace DocWatcher.Wpf.Validation;

public static class DocumentValidator
{
	public static List<string> Validate(DocumentInput input)
	{
		var errors = new List<string>();

		if (string.IsNullOrWhiteSpace(input.Titolo))
			errors.Add("Il titolo e' obbligatorio.");

		if (input.DataScadenza is null)
			errors.Add("La data di scadenza e' obbligatoria.");

		// In futuro puoi aggiungere altre regole qui:
		// - Data non nel passato
		// - Percorso allegato valido, ecc.

		return errors;
	}
}
