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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using ThreadedLockness.Threading;

namespace ThreadedLockness.Collections {
    /// <summary>
    ///     This class is intended to be usable without System Locks.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class ThreadSafeHashSet<TValue> : ThreadSafeObject {
        private readonly int _blockSize;
        private readonly Func<IFreeLock<long>> _idFactory;
        private readonly ThreadSafeLinkedList<ThreadSafeDynamicArray<HasValueTracker<TValue>>> _storage;

        public ThreadSafeHashSet(int blockSize = 8, Func<IFreeLock<long>> idFactory = null) {
            _storage = new ThreadSafeLinkedList<ThreadSafeDynamicArray<HasValueTracker<TValue>>>();
            _idFactory = idFactory ??
                         (() => new InterlockedSyncronizationHandle());
            _blockSize = blockSize;

            TryGrow_DoNotOuterLock(new HasValueTracker<TValue>());
        }

        public bool TryAdd(TValue v, int spinCount) {
            do {
                if (TryAdd(v))
                    return true;
            } while (spinCount-- > 0);

            return false;
        }

        public bool TryAdd(TValue v) {
            var visitationStatus = _storage.VisitWhile(DefaultInternalSpinCount, v, TrySetValueInNode);
            if (!visitationStatus.HasValue)
                throw new InvalidOperationException();

            if (visitationStatus.Value == LinkListVisitorStatus.CompletedWithoutSuccess)
                return TryGrow_DoNotOuterLock(new HasValueTracker<TValue>(v));

            return visitationStatus.Value == LinkListVisitorStatus.Success;
        }

        public int ForceCount() {
            var ret = new List<int>();
            _storage.VisitWhile(100,
                0,
                (ignoredState, block) => {
                    lock (ret) {
                        var sum = 0;
                        foreach (var entry in block.Value)
                            if (entry.HasValue)
                                sum++;

                        ret.Add(sum);
                    }

                    return LinkListVisitorStatus.Continue;
                });

            return ret.Sum();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private LinkListVisitorStatus TrySetValueInNode(TValue state,
            ThreadSafeLinkedList<ThreadSafeDynamicArray<HasValueTracker<TValue>>>.ThreadSafeLinkListNode blockNode) {
            if (blockNode == null)
                return LinkListVisitorStatus.CompletedWithoutSuccess;

            var id = state.GetHashCode();
            var block = blockNode.Value;
            if (block == null)
                throw new InvalidOperationException();

            LinkListVisitorStatus ret;
            //  don't assume the lock for _storage
            //  is the same lock for the the nodes
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var complex = block.PrepareCriticalExecutionRegionBlock()) {
                    if (complex.Initialize())
                        if (!complex.Storage[id % complex.Storage.Length].HasValue) {
                            complex.Storage[id % complex.Storage.Length].SetValueGuarded(state);
                            complex.SoftCommit();
                            ret = LinkListVisitorStatus.Success;
                        }
                        else {
                            ret = LinkListVisitorStatus.CompletedWithoutSuccess;
                        }
                    else
                        ret = LinkListVisitorStatus.Continue;
                }
            }


            return ret;
        }

        private bool TryGrow_DoNotOuterLock(HasValueTracker<TValue> initialValue) {
            var first = new ThreadSafeDynamicArray<HasValueTracker<TValue>>(_idFactory());
            if (first.TryResize(_blockSize, 2) != ThreadSafeArrayStates.Success)
                throw new InsufficientMemoryException();

            first.TrySet(initialValue.GetHashCode() % first.Count, initialValue);
            return _storage.TryAddLast(first, out _);
        }
    }
}