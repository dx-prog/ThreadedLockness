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
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using ThreadedLockness.Collections;

namespace ThreadedLockness.Threading {
    public sealed class InterlockedSyncronizationHandle : ThreadSafeObject, IFreeLock<long> {
        private long _currentLock = -1;
        private long _lockCount;

        public InterlockedSyncronizationHandle(bool debug = false) {
            Debug = debug;
        }

        public bool Debug { get; set; }


        /// <summary>
        ///     Acquire a new lock ID
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool AcquireLock(out long token) {
            var ret = false;
            var id = Thread.CurrentThread.ManagedThreadId;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                token = -1;
                if (-1 == Interlocked.CompareExchange(ref _currentLock, id, -1)) {
                    token = id;
                    Interlocked.Increment(ref _lockCount);
                    ret = true;
                }
                else if (id == Interlocked.CompareExchange(ref _currentLock, id, id)) {
                    // seperate branch so can verify via code coverage
                    token = id;
                    Interlocked.Increment(ref _lockCount);
                    ret = true;
                }
                if (ret)
                    Thread.BeginCriticalRegion();
            }
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public bool ReleaseLock(long token, bool assertIfFail = true) {
            var ret = false;
            var id = Thread.CurrentThread.ManagedThreadId;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                // check if we are same thread
                if (id == Interlocked.CompareExchange(ref _currentLock, id, id)) {
                    if (token != id)
                        throw new InvalidOperationException("E0: cross thread release");

                    if (Interlocked.Decrement(ref _lockCount) == 0) {
                        Thread.EndCriticalRegion();
                        // if the logic herein is correct, this should never happen
                        if (Interlocked.CompareExchange(ref _currentLock, -1, id) != id)
                            throw new InvalidOperationException();

                        ret = true;
                    }
                }

                if (ret == false && assertIfFail)
                    throw new InvalidOperationException($"E1: cross thread release {id} from {token}");
            }

            return ret;
        }
    }
}