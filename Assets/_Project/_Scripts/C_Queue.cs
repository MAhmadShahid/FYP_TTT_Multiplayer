using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace TicTacToe
{
    public class C_Queue<T>
    {
        LinkedList<T> _queue;

        public C_Queue() {
            _queue = new LinkedList<T>();
        }

        public bool Remove(T item)
        {
            return _queue.Remove(item);
        }

        public void Enqueue(T item)
        {
            _queue.AddFirst(item);
        }

        public T Dequeue()
        {
            T item = _queue.First.Value;
            return item;
        }

        public bool TryDequeue(out T item)
        {
            item = _queue.First.Value;
            return item != null;
        }
    }

}

