namespace ApiFluentValidator.BackgroundServices;

public interface IBackgroundQueue<T>
{
    void Enqueue(T item);
    T Dequeue();
}
