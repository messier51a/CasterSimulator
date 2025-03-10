using System.Collections.Concurrent;

namespace CasterSimulator.Common.Collections;

public class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
{
    public event Action? CollectionChanged;

    // Default constructor (empty queue)
    public ObservableConcurrentQueue()
    {
    }

    // Constructor that initializes with a collection
    public ObservableConcurrentQueue(IEnumerable<T> collection) : base(collection)
    {
        CollectionChanged?.Invoke(); // Notify initial state
    }

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        CollectionChanged?.Invoke();
    }

    public new bool TryDequeue(out T? item)
    {
        var result = base.TryDequeue(out item);
        if (result)
        {
            CollectionChanged?.Invoke();
        }

        return result;
    }

    public void ReplaceAll(IEnumerable<T> newItems)
    {
        while (base.TryDequeue(out _))
        {
        } // Clear the queue without triggering events

        foreach (var item in newItems)
        {
            base.Enqueue(item);
        }

        CollectionChanged?.Invoke(); // Trigger only once
    }
}