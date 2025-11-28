namespace DocWatcher.Wpf.DTO;

public class TagFilterItem
{
    public string Nome { get; }
    public bool IsSelected { get; set; }

    public TagFilterItem(string nome)
    {
        Nome = nome;
    }
}
