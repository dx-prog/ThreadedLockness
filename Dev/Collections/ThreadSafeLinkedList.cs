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
using ThreadedLockness.Threading;

namespace ThreadedLockness.Collections {
    public class ThreadSafeLinkedList<TValue> : ThreadSafeObject,
        IEnumerable<ThreadSafeLinkedList<TValue>.ThreadSafeLinkListNode> {
        private readonly AtomicCounter _counter = new AtomicCounter(0);

        public ThreadSafeLinkedList(IFreeLock<long> lockSystem = null) {
            LockSystem = lockSystem ?? new InterlockedSyncronizationHandle();
        }

        public IFreeLock<long> LockSystem { get; }
        public ThreadSafeLinkListNode First { get; private set; }
        public ThreadSafeLinkListNode Last { get; private set; }
        public long Count => _counter.TransientValue;

        public IEnumerator<ThreadSafeLinkListNode> GetEnumerator() {
            using (var t = LockSystem.TryGetLock()) {
                // ReSharper disable once AssignmentInConditionalExpression
                if (!t.Initialize(5))
                    yield break;

                var pos = First;
                while (pos != null) {
                    yield return pos;

                    pos = pos.Next;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public override string ToString() {
            return $"First={First},Last={Last}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool TryAddBefore(ThreadSafeLinkListNode start,
            TValue value,
            out ThreadSafeLinkListNode newNode,
            int spintCount = 100) {
            var success = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                newNode = null;
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (success = t.Initialize(spintCount)) {
                        var isFirst = start == First;
                        var oldPrevious = start?.Previous;
                        if (start != null && start.Owner != this)
                            throw new InvalidOperationException();
                        if (oldPrevious == null && !isFirst)
                            throw new InvalidOperationException();
                        // this might happen if the node structure is corrupted
                        var toInsert = new ThreadSafeLinkListNode(this) {
                            Value = value
                        };
                        if (null == First) {
                            First = Last = toInsert;
                        }
                        else {
                            Join(start?.Previous, toInsert, start);
                            if (isFirst)
                                First = toInsert;
                            if (Last == null)
                                Last = toInsert;
                        }
                        newNode = toInsert;
                        _counter.Increment();
                    }
                }
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool TryAddAfter(ThreadSafeLinkListNode start,
            TValue value,
            out ThreadSafeLinkListNode newNode,
            int spintCount = 100) {
            var success = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                newNode = null;
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (success = t.Initialize(spintCount)) {
                        var isLast = start == Last;

                        var oldNext = start?.Next;
                        if (start != null && start.Owner != this)
                            throw new InvalidOperationException();
                        if (oldNext == null && !isLast)
                            throw new InvalidOperationException();
                        // this might happen if the node structure is corrupted
                        var toInsert = newNode = new ThreadSafeLinkListNode(this) {
                            Value = value
                        };
                        if (null == Last) {
                            First = Last = toInsert;
                        }
                        else {
                            Join(start, toInsert, start?.Next);

                            if (isLast)
                                Last = toInsert;
                            if (First == null)
                                First = toInsert;
                        }
                        _counter.Increment();
                    }
                }
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool RemoveFirst(int spinCount, out ThreadSafeLinkListNode outFirst) {
            var success = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                outFirst = null;
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (success = t.Initialize(spinCount)) {
                        outFirst = First;
                        if (First == Last) {
                            First = Last = null;
                        }
                        else {
                            var oldFirst = First;
                            First = oldFirst.Next;
                            if (First == null)
                                Last = null;
                        }
                        outFirst.Next = outFirst.Previous = null;
                        _counter.Decrement();
                    }
                }
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public ThreadSafeLinkListNode TryGetAt(int position, int spinCount = 5) {
            ThreadSafeLinkListNode ret = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (t.Initialize(spinCount)) {
                        var count = 0;
                        var start = First;
                        if (position == -1) {
                            ret = Last;
                        }
                        else {
                            while (count < position) {
                                if (start == null)
                                    break;

                                start = start.Next;
                                count++;
                            }

                            ret = start;
                        }
                    }
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public LinkListVisitorStatus? VisitWhile<TState>(int spinCount,
            TState state,
            Func<TState, ThreadSafeLinkListNode, LinkListVisitorStatus> visitor,
            bool prepareDelegate = true) {
            LinkListVisitorStatus? ret = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                if (prepareDelegate)
                    RuntimeHelpers.PrepareContractedDelegate(visitor);
            }
            finally {
                using (var @lock = LockSystem.TryGetLock()) {
                    if (@lock.Initialize(spinCount)) {
                        var tmp = First;
                        if (First == null) {
                            ret = LinkListVisitorStatus.CompletedWithoutSuccess;
                        }
                        else {
                            do {
                                switch (visitor(state, tmp)) {
                                    case LinkListVisitorStatus.Continue:
                                        ret = LinkListVisitorStatus.Continue;
                                        tmp = tmp.Next;
                                        continue;
                                    case LinkListVisitorStatus.CompletedWithoutSuccess:
                                        ret = LinkListVisitorStatus.CompletedWithoutSuccess;
                                        tmp = null;
                                        break;
                                    case LinkListVisitorStatus.Success:
                                        ret = LinkListVisitorStatus.Success;
                                        tmp = null;
                                        break;
                                    case LinkListVisitorStatus.Failure:
                                        ret = LinkListVisitorStatus.Failure;
                                        tmp = null;
                                        break;
                                    default:
                                        throw new InvalidOperationException();
                                }
                            } while (tmp != null);

                            if (ret == LinkListVisitorStatus.Continue)
                                ret = LinkListVisitorStatus.CompletedWithoutSuccess;
                        }
                    }
                    else {
                        ret = LinkListVisitorStatus.Failure;
                    }
                }
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool RemoveLast(int spinCount, out ThreadSafeLinkListNode outLast) {
            bool success;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                outLast = null;
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (success = t.Initialize(spinCount)) {
                        outLast = Last;
                        if (First == Last) {
                            First = Last = null;
                        }
                        else {
                            Last = Last.Previous;
                            Last.Next = null;
                            outLast.Next = outLast.Previous = null;
                        }

                        _counter.Decrement();
                    }
                }
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool TryAddLast(TValue newValue, out ThreadSafeLinkListNode result, int spinCount = 100) {
            bool success;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                result = null;
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    success = t.Initialize(spinCount);
                    if (success) {
                        if (null == First) {
                            Last = First = result = new ThreadSafeLinkListNode(this) {
                                Value = newValue
                            };
                        }
                        else {
                            var oldLast = Last;
                            Last = oldLast.Next = result = new ThreadSafeLinkListNode(this) {
                                Previous = oldLast,
                                Value = newValue
                            };
                        }

                        _counter.Increment();
                    }
                }
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool TryAddFirst(TValue newValue, out ThreadSafeLinkListNode result, int spinCount = 100) {
            var success = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                result = null;
            }
            finally {
                using (var t = LockSystem.TryGetLock()) {
                    success = t.Initialize(spinCount);
                    if (success) {
                        result = new ThreadSafeLinkListNode(this) {
                            Value = newValue
                        };
                        if (null == First) {
                            First = Last = result;
                        }
                        else {
                            var oldFirst = First;
                            First = result;
                            First.Next = oldFirst;
                            oldFirst.Previous = First;
                        }

                        _counter.Increment();
                    }
                }
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Join(ThreadSafeLinkListNode a, ThreadSafeLinkListNode b, ThreadSafeLinkListNode c) {
            if (a != null) {
                a.Next = b;
                b.Previous = a;
            }
            if (c == null)
                return;

            c.Previous = b;
            b.Next = c;
        }


        public class ThreadSafeLinkListNode : ThreadSafeObject {
            public ThreadSafeLinkListNode(ThreadSafeLinkedList<TValue> root) {
                Owner = root;
            }

            public ThreadSafeLinkedList<TValue> Owner { get; }
            public TValue Value { get; set; }
            public ThreadSafeLinkListNode Next { get; internal set; }
            public ThreadSafeLinkListNode Previous { get; internal set; }

            public override int GetHashCode() {
                return (Owner.GetHashCode() << 7) ^ RuntimeHelpers.GetHashCode(this);
            }

            public override string ToString() {
                return $"[{Owner?.Id}]={{Next={Next?.Id},Previous={Previous?.Id}}}";
            }
        }
    }
}