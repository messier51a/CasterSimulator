using System.Collections.Concurrent;

namespace CasterSimulator.Common.Collections;

/// <summary>
/// Extends the standard ConcurrentQueue with notification capabilities when the collection changes.
/// This allows other components to observe and react to changes in the queue without polling.
/// </summary>
/// <typeparam name="T">The type of elements contained in the queue.</typeparam>
public class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
{
    /// <summary>
    /// Event that is triggered whenever the queue's contents change (items added or removed).
    /// </summary>
    public event Action? CollectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableConcurrentQueue{T}"/> class.
    /// Creates an empty observable concurrent queue.
    /// </summary>
    public ObservableConcurrentQueue()
    {
        CollectionChanged?.Invoke();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableConcurrentQueue{T}"/> class.
    /// Creates an observable concurrent queue containing the elements from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new observable concurrent queue.</param>
    /// <exception cref="ArgumentNullException">The collection is null.</exception>
    public ObservableConcurrentQueue(IEnumerable<T> collection) : base(collection)
    {
        CollectionChanged?.Invoke(); // Notify initial state
    }

    /// <summary>
    /// Adds an item to the end of the queue and triggers the CollectionChanged event.
    /// </summary>
    /// <param name="item">The item to add to the queue.</param>
    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        CollectionChanged?.Invoke();
    }

    /// <summary>
    /// Tries to remove and return the item at the beginning of the queue.
    /// Triggers the CollectionChanged event if successful.
    /// </summary>
    /// <param name="item">
    /// When this method returns, if the operation was successful, item contains the object removed.
    /// If no object was available to be removed, the value is unspecified.
    /// </param>
    /// <returns>
    /// true if an element was removed and returned from the beginning of the queue successfully;
    /// otherwise, false.
    /// </returns>
    public new bool TryDequeue(out T? item)
    {
        var result = base.TryDequeue(out item);
        if (result)
        {
            CollectionChanged?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// Replaces all items in the queue with a new collection of items.
    /// Clears the existing queue contents and adds all items from the provided collection.
    /// Triggers the CollectionChanged event only once after all operations are complete.
    /// </summary>
    /// <param name="newItems">The new collection of items to place in the queue.</param>
    /// <remarks>
    /// This operation is not atomic. If concurrent operations occur during execution,
    /// the queue might end up in an unexpected state. Use with caution in multithreaded scenarios.
    /// </remarks>
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