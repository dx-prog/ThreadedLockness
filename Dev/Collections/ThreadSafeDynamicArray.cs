/* Project: Lockless Data Type Research Project - ThreadedLockness
 * 
 *                                  MIT LICENSE + WARNING
 * Copyright 2017 David Garcia
 * All Rights Reserved
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * WARNING:
 * THE SOFTWARE IS EXPERIMENTAL
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using ThreadedLockness.Threading;

namespace ThreadedLockness.Collections {
    public class ThreadSafeDynamicArray<T> : ThreadSafeObject, IEnumerable<T> {
        private long _lockFails;
        private T[] _storage = new T[16];

        public ThreadSafeDynamicArray() {
            LockSystem = new InterlockedSyncronizationHandle();
        }

        public ThreadSafeDynamicArray(IFreeLock<long> @lock) {
            LockSystem = @lock;
        }

        public IFreeLock<long> LockSystem { get; }

        public long LockFails => _lockFails;
        public long Count { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IEnumerator<T> GetEnumerator() {
            var ret = new HasValueTracker<T>();
            for (var i = 0; i < Count; i++) {
                ret.ClearValue();
                while (true) {
                    try {
                    }
                    finally {
                        var tryGetResults = TryGet(i, out var tmp);
                        if (tryGetResults == ThreadSafeArrayStates.Success)
                            ret.SetValueGuarded(tmp);
                    }
                    if (ret.HasValue)
                        yield return ret.Value;

                    break;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public override string ToString() {
            return $"{Id}, Count={Count}, Fails={LockFails}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryExchange(long index, T input, ref T output) {
            var ret = ThreadSafeArrayStates.BadInput;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var tryLock = LockSystem.TryGetLock()) {
                    if (tryLock.Initialize(DefaultInternalSpinCount))
                        try {
                        }
                        finally {
                            if (UnsafeCheckofBounds(index)) {
                                var tmp = _storage[index];
                                _storage[index] = input;
                                output = tmp;

                                ret = ThreadSafeArrayStates.Success;
                            }
                        }
                    else
                        MarkLockFail(ref ret);
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TrySet(long index, T input) {
            var ret = ThreadSafeArrayStates.BadInput;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var tryLock = LockSystem.TryGetLock()) {
                    if (tryLock.Initialize(DefaultInternalSpinCount))
                        try {
                        }
                        finally {
                            if (UnsafeCheckofBounds(index)) {
                                _storage[index] = input;
                                ret = ThreadSafeArrayStates.Success;
                            }
                        }
                    else
                        MarkLockFail(ref ret);
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryGet(long index, out T result) {
            result = default(T);
            var ret = ThreadSafeArrayStates.BadInput;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var tryLock = LockSystem.TryGetLock()) {
                    if (tryLock.Initialize(DefaultInternalSpinCount))
                        try {
                        }
                        finally {
                            if (UnsafeCheckofBounds(index)) {
                                result = _storage[index];
                                ret = ThreadSafeArrayStates.Success;
                            }
                        }
                    else
                        MarkLockFail(ref ret);
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryRemove(long index, int spinCount) {
            var ret = ThreadSafeArrayStates.BadInput;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                do {
                    var tmp = TryRemove(index);
                    if (tmp == ThreadSafeArrayStates.LockFailed)
                        continue;

                    if (tmp == ThreadSafeArrayStates.BadInput)
                        break;

                    ret = ThreadSafeArrayStates.Success;
                    break;
                } while (spinCount-- > 0);
            }

            return ret;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryRemove(long index) {
            if (index < 0)
                throw new ArgumentOutOfRangeException();

            var ret = ThreadSafeArrayStates.BadInput;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var tryLock = LockSystem.TryGetLock()) {
                    if (tryLock.Initialize(DefaultInternalSpinCount))
                        try {
                        }
                        finally {
                            if (UnsafeCheckofBounds(index)) {
                                var end = Count;
                                for (var s = index; s < end - 1; s++)
                                    _storage[s] = _storage[s + 1];

                                Count--;
                                ret = ThreadSafeArrayStates.Success;
                            }
                            else {
                                ret = ThreadSafeArrayStates.BadInput;
                            }
                        }
                    else
                        MarkLockFail(ref ret);
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryAdd(T t, int spinCount) {
            var success = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                do {
                    var result = TryAdd(t);
                    if (result == ThreadSafeArrayStates.LockFailed)
                        continue;

                    success = true;
                    break;
                } while (spinCount-- > 0);
            }

            return success ? ThreadSafeArrayStates.Success : ThreadSafeArrayStates.LockFailed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryAdd(T t) {
            var ret = ThreadSafeArrayStates.BadInput;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var tryLock = LockSystem.TryGetLock()) {
                    if (tryLock.Initialize(DefaultInternalSpinCount))
                        try {
                        }
                        finally {
                            if (Count + 1 >= _storage.Length)
                                Array.Resize(ref _storage, _storage.Length + 16);
                            _storage[Count] = t;
                            Count++;
                            ret = ThreadSafeArrayStates.Success;
                        }
                    else
                        MarkLockFail(ref ret);
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeArrayStates TryResize(long storageCount, int roundTo) {
            var ret = ThreadSafeArrayStates.BadInput;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var tryLock = LockSystem.TryGetLock()) {
                    if (tryLock.Initialize(DefaultInternalSpinCount))
                        try {
                        }
                        finally {
                            ResizeToBoundary(storageCount, roundTo, ref _storage);

                            Count = _storage.Length;
                            ret = ThreadSafeArrayStates.Success;
                        }
                    else
                        MarkLockFail(ref ret);
                }
            }

            return ret;
        }

        public static void ResizeToBoundary(long storageCount, int roundTo, ref T[] storage) {
            var lenA = RoundUpTo(storage.LongLength, roundTo);
            var lenB = RoundUpTo(storageCount, roundTo);
            var sz = Math.Max(Math.Max(lenA, lenB), roundTo);
            Array.Resize(ref storage, checked((int) sz));
        }

        public TransactionalOperationContext PrepareCriticalExecutionRegionBlock() {
            return new TransactionalOperationContext(this);
        }

        internal void MarkLockFail(ref ThreadSafeArrayStates ret) {
            ret = ThreadSafeArrayStates.LockFailed;
            Interlocked.Increment(ref _lockFails);
        }

        private bool UnsafeCheckofBounds(long index) {
            return index >= 0 && index < Count;
        }

        private static long RoundUpTo(long len, long roundTo) {
            var lower = len / roundTo * roundTo;
            var uppder = len % roundTo > 0 ? roundTo : 0;
            return lower + uppder;
        }

        public class TransactionalOperationContext : AutoLock<long> {
            private readonly ThreadSafeDynamicArray<T> _src;
            private long _defaultSize;

            public TransactionalOperationContext(ThreadSafeDynamicArray<T> src) :
                base(src.LockSystem, src.GetType().Name) {
                _src = src;
            }

            public long Count { get; set; }
            public T[] Storage { get; set; }
            public bool?DoSoftSave { get; private set; }

            public void SoftCommit() {
                DoSoftSave = false;
            }

            public override bool Initialize(int spinCount = 100, string caller = null) {
                RuntimeHelpers.ProbeForSufficientStack();
                if (!base.Initialize(spinCount, caller))
                    return false;

                Count = _defaultSize = _src.Count;
                Storage = _src._storage;
                DoSoftSave = false;
                return true;
            }

            public override void Dispose() {
                if (DoSoftSave == true) {
                    _src.Count = Count;
                    _src._storage = Storage ?? new T[_defaultSize];
                }
                base.Dispose();
            }
        }
    }
}