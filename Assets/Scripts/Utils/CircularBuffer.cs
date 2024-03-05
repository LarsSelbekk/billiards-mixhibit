using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// simplified version of https://github.com/cyotek/Cyotek.Collections.Generic.CircularBuffer/blob/f8f825b5db8cd5970cf61f469a1f8251663351e7/src/CircularBuffer.cs

namespace Utils
{
  /// <summary>
  /// Represents a first-in, first-out collection of objects using a fixed buffer and automatic overwrite support.
  /// </summary>
  /// <typeparam name="T">Specifies the type of elements in the buffer.</typeparam>
  /// <remarks>
  /// <para>The capacity of a <see cref="CircularBuffer{T}" /> is the number of elements the <see cref="CircularBuffer{T}"/> can
  /// hold. If an attempt is made to put more items in the buffer than available capacity, items at the start of the buffer are
  /// automatically overwritten. This behavior can be modified via the <see cref="AllowOverwrite"/> property.</para>
  /// <para>CircularBuffer{T} accepts <c>null</c> as a valid value for reference types and allows duplicate elements.</para>
  /// <para>The <see cref="Get()"/> methods will remove the items that are returned from the CircularBuffer{T}. To view the contents of the CircularBuffer{T} without removing items, use the <see cref="Peek()"/> or <see cref="PeekLast()"/> methods.</para>
  /// </remarks>
  public class CircularBuffer<T> : ICollection<T>, ICollection
  {
    #region Private Fields

    private T[] _buffer;

    private int _capacity;

    [NonSerialized]
    private object _syncRoot;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class that is empty and has the specified initial capacity and overwrite behavior.
    /// </summary>
    /// <param name="capacity">The maximum capacity of the buffer.</param>
    /// <param name="allowOverwrite">If set to <c>true</c> the buffer will automatically overwrite the oldest items when full.</param>
    /// <exception cref="System.ArgumentException">Thrown if the <paramref name="capacity"/> is less than zero.</exception>
    public CircularBuffer(int capacity, bool allowOverwrite = true)
    {
      if (capacity < 0)
      {
        throw new ArgumentException("The buffer capacity must be greater than or equal to zero.", nameof(capacity));
      }

      Size = 0;
      Capacity = capacity;
      AllowOverwrite = allowOverwrite;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets or sets a value indicating whether the buffer will automatically overwrite the oldest items in the buffer when the maximum capacity is reached.
    /// </summary>
    /// <value><c>true</c> if the oldest items in the buffer are automatically overwritten when the buffer is full; otherwise, <c>false</c>.</value>
    private bool AllowOverwrite { get; }

    /// <summary>
    /// Gets or sets the total number of elements the internal data structure can hold.
    /// </summary>
    /// <value>The total number of elements that the <see cref="CircularBuffer{T}"/> can contain.</value>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the specified new capacity is smaller than the current contents of the buffer.</exception>
    public int Capacity
    {
      get => _capacity;
      set
      {
        if (value == _capacity) return;
        if (value < Size)
        {
          throw new ArgumentOutOfRangeException(nameof(value), value, "The new capacity must be greater than or equal to the buffer size.");
        }

        var newBuffer = new T[value];
        if (Size > 0)
        {
          CopyTo(newBuffer);
        }

        _buffer = newBuffer;
        Tail = Size;
        Head = 0;

        _capacity = value;
      }
    }

    /// <summary>
    /// Gets the index of the beginning of the buffer data.
    /// </summary>
    /// <value>The index of the first element in the buffer.</value>
    public int Head { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the buffer is empty.
    /// </summary>
    /// <value><c>true</c> if buffer is empty; otherwise, <c>false</c>.</value>
    public virtual bool IsEmpty => Size == 0;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <value>The number of elements contained in the <see cref="CircularBuffer{T}"/>.</value>
    public int Size { get; private set; }

    /// <summary>
    /// Gets the index of the end of the buffer data.
    /// </summary>
    /// <value>The index of the last element in the buffer.</value>
    public int Tail { get; protected set; }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="ICollection" />.
    /// </summary>
    /// <value>The number of elements actually contained in the <see cref="ICollection" />.</value>
    int ICollection.Count => Size;

    /// <summary>
    /// Gets the number of elements contained in the <see cref="ICollection{T}" />.
    /// </summary>
    /// <value>The number of elements actually contained in the <see cref="ICollection{T}" />.</value>
    int ICollection<T>.Count => Size;

    /// <summary>
    /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
    /// </summary>
    /// <value><c>true</c> if the <see cref="ICollection{T}"/> is read-only; otherwise, <c>false</c>. In the default implementation of <see cref="CircularBuffer{T}"/>, this property always returns <c>false</c>.</value>
    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
    /// </summary>
    /// <value><c>true</c> if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, <c>false</c>. In the default implementation of <see cref="CircularBuffer{T}"/>, this property always returns <c>false</c>.</value>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
    /// </summary>
    /// <value>An object that can be used to synchronize access to the <see cref="ICollection"/></value>
    object ICollection.SyncRoot
    {
      get
      {
        if (_syncRoot == null)
        {
          Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
        }

        return _syncRoot;
      }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Removes all items from the <see cref="CircularBuffer{T}" />.
    /// </summary>
    public void Clear()
    {
      Size = 0;
      Head = 0;
      Tail = 0;
      _buffer = new T[_capacity];
    }

    /// <summary>
    /// Determines whether the <see cref="CircularBuffer{T}" /> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="CircularBuffer{T}" />.</param>
    /// <returns><c>true</c> if <paramref name="item" /> is found in the <see cref="CircularBuffer{T}" />; otherwise, <c>false</c>.</returns>
    public bool Contains(T item)
    {
      var bufferIndex = Head;
      var comparer = EqualityComparer<T>.Default;
      var result = false;

      for (var i = 0; i < Size; i++, bufferIndex++)
      {
        if (bufferIndex == _capacity)
        {
          bufferIndex = 0;
        }

        // ReSharper disable CompareNonConstrainedGenericWithNull
        if (item == null && _buffer[bufferIndex] == null || _buffer[bufferIndex] != null && comparer.Equals(_buffer[bufferIndex], item))
        {
          result = true;
          break;
        }
        // ReSharper restore CompareNonConstrainedGenericWithNull
      }

      return result;
    }

    /// <summary>
    /// Copies a range of elements from the <see cref="CircularBuffer{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
    /// </summary>
    /// <param name="index">The zero-based index in the source <see cref="CircularBuffer{T}"/> at which copying begins.</param>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="CircularBuffer{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    public virtual void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
      if (count > Size)
      {
        throw new ArgumentOutOfRangeException(nameof(count), count, "The read count cannot be greater than the buffer size.");
      }

      var bufferIndex = Head + index;

      for (var i = 0; i < count; i++, bufferIndex++, arrayIndex++)
      {
        if (bufferIndex >= _capacity)
        {
          bufferIndex -= _capacity;
        }
        array[arrayIndex] = _buffer[bufferIndex];
      }
    }

    /// <summary>
    /// Copies the entire <see cref="CircularBuffer{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="CircularBuffer{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex = 0)
    {
      CopyTo(0, array, arrayIndex, Math.Min(Size, array.Length - arrayIndex));
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <returns>The object that is removed from the beginning of the <see cref="CircularBuffer{T}"/>.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
    /// <remarks>This method is similar to the <see cref="Peek()"/> method, but <c>Peek</c> does not modify the <see cref="CircularBuffer{T}"/>.</remarks>
    public virtual T Get()
    {
      if (IsEmpty)
      {
        throw new InvalidOperationException("The buffer is empty.");
      }

      var item = _buffer[Head];
      if (++Head == _capacity)
      {
        Head = 0;
      }
      Size--;

      return item;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <returns>A <see cref="IEnumerator{T}"/> for the <see cref="CircularBuffer{T}"/>.</returns>
    public IEnumerator<T> GetEnumerator()
    {
      var bufferIndex = Head;

      for (var i = 0; i < Size; i++, bufferIndex++)
      {
        if (bufferIndex == _capacity)
        {
          bufferIndex = 0;
        }

        yield return _buffer[bufferIndex];
      }
    }

    /// <summary>
    /// Removes and returns the object at the end of the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <returns>The object that is removed from the end of the <see cref="CircularBuffer{T}"/>.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
    /// <remarks>This method is similar to the <see cref="PeekLast()"/> method, but <c>PeekLast</c> does not modify the <see cref="CircularBuffer{T}"/>.</remarks>
    public virtual T GetLast()
    {
      if (IsEmpty)
      {
        throw new InvalidOperationException("The buffer is empty.");
      }

      var index = GetTailIndex(0);
      var item = _buffer[index];

      if (--Tail < 0)
      {
        Tail = 0;
      }
      Size--;

      return item;
    }

    /// <summary>
    /// Returns the object at the beginning of the <see cref="CircularBuffer{T}"/> without removing it.
    /// </summary>
    /// <returns>The object at the beginning of the <see cref="CircularBuffer{T}"/>.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
    public virtual T Peek()
    {
      if (IsEmpty)
      {
        throw new InvalidOperationException("The buffer is empty.");
      }

      return _buffer[Head];
    }

    /// <summary>
    /// Returns the specified number of objects from the beginning of the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <param name="count">The number of elements to return from the <see cref="CircularBuffer{T}"/>.</param>
    /// <returns>The objects that from the beginning of the <see cref="CircularBuffer{T}"/>.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
    public virtual T[] Peek(int count)
    {
      if (IsEmpty)
      {
        throw new InvalidOperationException("The buffer is empty.");
      }

      var items = new T[count];
      CopyTo(items);

      return items;
    }

    /// <summary> Returns the object at the specified location in the <see cref="CircularBuffer{T}"/> without removing it. </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the buffer is empty. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown when one or more arguments are outside
    ///  the required range. </exception>
    /// <param name="index"> The zero-based index in the source <see cref="CircularBuffer{T}"/> of the item to retrieve. </param>
    public T PeekAt(int index)
    {
      if (IsEmpty)
      {
        throw new InvalidOperationException("The buffer is empty.");
      }

      if (index < 0 || index >= Size)
      {
        throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and {Size}.");
      }

      return _buffer[GetHeadIndex(index)];
    }

    /// <summary>
    /// Returns the object at the end of the <see cref="CircularBuffer{T}"/> without removing it.
    /// </summary>
    /// <returns>The object at the end of the <see cref="CircularBuffer{T}"/>.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the buffer is empty.</exception>
    public virtual T PeekLast()
    {
      if (IsEmpty)
      {
        throw new InvalidOperationException("The buffer is empty.");
      }

      var index = GetTailIndex(0);
      var item = _buffer[index];

      return item;
    }

    /// <summary>
    /// Adds an object to the end of the <see cref="CircularBuffer{T}"/>.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="CircularBuffer{T}"/>. The value can be <c>null</c> for reference types.</param>
    /// <exception cref="System.InvalidOperationException">Thrown if buffer does not have sufficient capacity to put in new items.</exception>
    /// <remarks>If <see cref="Size"/> already equals the capacity and the <see cref="AllowOverwrite"/> property is <c>true</c>, the oldest item in the <see cref="CircularBuffer{T}"/> is overwritten with <paramref name="item"/>.</remarks>
    public virtual void Put(T item)
    {
      if (!AllowOverwrite && Size == _capacity)
      {
        throw new InvalidOperationException("The buffer does not have sufficient capacity to put new items.");
      }

      _buffer[Tail] = item;

      Tail++;
      if (Size == _capacity)
      {
        Head++;
        if (Head >= _capacity)
        {
          Head -= _capacity;
        }
      }

      if (Tail == _capacity)
      {
        Tail = 0;
      }

      if (Size != _capacity)
      {
        Size++;
      }
    }

    /// <summary>
    /// Copies the <see cref="CircularBuffer{T}"/> elements to a new array.
    /// </summary>
    /// <returns>A new array containing elements copied from the <see cref="CircularBuffer{T}"/>.</returns>
    /// <remarks>The <see cref="CircularBuffer{T}"/> is not modified. The order of the elements in the new array is the same as the order of the elements from the beginning of the <see cref="CircularBuffer{T}"/> to its end.</remarks>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public T[] ToArray()
    {
      var result = new T[Size];

      CopyTo(result);

      return result;
    }

    /// <summary>
    /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
    void ICollection<T>.Add(T item)
    {
      Put(item);
    }

    /// <summary>
    /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    void ICollection.CopyTo(Array array, int arrayIndex)
    {
      CopyTo((T[])array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
    /// <returns><c>true</c> if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <c>false</c>. This method also returns <c>false</c> if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
    /// <exception cref="System.NotSupportedException">Cannot remove items from collection.</exception>
    bool ICollection<T>.Remove(T item)
    {
      throw new NotSupportedException("Cannot remove items from collection.");
    }

    #endregion Public Methods

    #region Private Methods

    private int GetHeadIndex(int index)
    {
      var newIndex = Head + index;

      if (newIndex >= _capacity)
      {
        newIndex -= _capacity;
      }

      return newIndex;
    }

    private int GetTailIndex(int index)
    {
      var bufferIndex = Tail == 0
        ? Size - (index + 1)
        : Tail - (index + 1);

      if (bufferIndex < 0)
      {
        bufferIndex += _capacity;
      }

      return bufferIndex;
    }

    #endregion Private Methods
  }
}