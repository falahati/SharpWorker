using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpWorker.Log
{
    internal class LogHistory : IReadOnlyCollection<LogRecord>
    {
        private readonly Queue<LogRecord> _queue = new Queue<LogRecord>();
        private int _maxCapacity;

        public LogHistory(int capacity)
        {
            MaxCapacity = capacity;
        }

        public int MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                lock (this)
                {
                    _maxCapacity = value;

                    while (_queue.Count > value)
                    {
                        _queue.Dequeue();
                    }
                }
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <inheritdoc />
        public IEnumerator<LogRecord> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        /// <inheritdoc />
        public int Count
        {
            get => _queue.Count;
        }

        public void AddToHistory(LogRecord record)
        {
            lock (this)
            {
                while (_queue.Count >= MaxCapacity)
                {
                    _queue.Dequeue();
                }

                _queue.Enqueue(record);
            }
        }
    }
}