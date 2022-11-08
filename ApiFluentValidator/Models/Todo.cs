namespace ApiFluentValidator.Models;

public class Todo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset? CompletedTimestamp { get; set; }
}
