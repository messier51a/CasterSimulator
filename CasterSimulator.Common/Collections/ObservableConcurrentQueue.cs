using System.Collections.Concurrent;

namespace CasterSimulator.Common.Collections;

public class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
{
    private bool _suppressEvents = false;
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
        if (!_suppressEvents)
            CollectionChanged?.Invoke();
    }

    public new bool TryDequeue(out T? item)
    {
        var result = base.TryDequeue(out item);
        if (result && !_suppressEvents)
        {
            CollectionChanged?.Invoke();
        }

        return result;
    }

    public void ReplaceAll(IEnumerable<T> newItems)
    {
        _suppressEvents = true;

        while (TryDequeue(out _))
        {
        } // Clear the queue without triggering events

        foreach (var item in newItems)
        {
            base.Enqueue(item);
        }

        _suppressEvents = false;
        
        CollectionChanged?.Invoke(); // Trigger only once
    }
}