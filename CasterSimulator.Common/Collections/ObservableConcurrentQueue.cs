using System.Collections.Concurrent;

namespace CasterSimulator.Common.Collections;

public class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
{
    public event Action? CollectionChanged;

    // Default constructor (empty queue)
    public ObservableConcurrentQueue() { }

    // Constructor that initializes with a collection
    public ObservableConcurrentQueue(IEnumerable<T> collection) : base(collection)
    {
        CollectionChanged?.Invoke(); // Notify initial state
    }

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        CollectionChanged?.Invoke(); // Notify listeners
    }

    public bool TryDequeue(out T item)
    {
        bool result = base.TryDequeue(out item);
        if (result)
        {
            CollectionChanged?.Invoke(); // Notify on removal
        }
        return result;
    }
}