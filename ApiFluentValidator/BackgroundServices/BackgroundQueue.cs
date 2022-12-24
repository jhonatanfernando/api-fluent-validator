using System.Collections.Concurrent;

namespace ApiFluentValidator.BackgroundServices;

public class BackgroundQueue<T> : IBackgroundQueue<T> where T : class
{
    private readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();

    public void Enqueue(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        _items.Enqueue(item);
    }

    public T Dequeue()
    {
        var success = _items.TryDequeue(out var workItem);

        return success
            ? workItem
            : null;
    }
}
