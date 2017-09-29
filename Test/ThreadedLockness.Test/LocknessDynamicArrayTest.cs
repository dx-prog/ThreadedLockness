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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadedLockness.Collections;
using ThreadedLockness.Threading;

namespace ThreadedLockness.Test {
    [TestClass]
    public class LocknessDynamicArrayTest : LocknessTestbase {
        [TestMethod]
        public void LocknessDynamicArray_Add() {
            var lda = new ThreadSafeDynamicArray<int>();

            TestAdd(lda, 10, 1);
            TestAdd(lda, 20, 2);
            TestAdd(lda, 30, 3);
            TestAdd(lda, 40, 4);
        }

        [TestMethod]
        public void LocknessDynamicArray_Remove() {
            var lda = new ThreadSafeDynamicArray<int>();

            TestAdd(lda, 10, 1);
            TestAdd(lda, 20, 2);
            TestAdd(lda, 30, 3);
            TestAdd(lda, 40, 4);

            TestRemove(lda, 0, 3, 20);
            TestRemove(lda, 0, 2, 30);
            TestRemove(lda, 0, 1, 40);
            TestRemove(lda, 0, 0, null);
        }


        [TestMethod]
        public void LocknessDynamicArray_ThreadedAdd_20() {
            var lda = new ThreadSafeDynamicArray<int>();
            var startCount = new AtomicCounter();
            var finCount = new AtomicCounter();

            TestThreads(20, startCount, lda, finCount);
        }

        [TestMethod]
        public void LocknessDynamicArray_ThreadedAdd_100x1000() {
            var lda = new ThreadSafeDynamicArray<int>();
            var startCount = new AtomicCounter();
            var finCount = new AtomicCounter();

            TestThreads(100, startCount, lda, finCount, 1000);
        }

        private static void TestThreads(int threadCount,
            AtomicCounter startCount,
            ThreadSafeDynamicArray<int> lda,
            AtomicCounter finCount,
            int scaler = 100) {
            var _counts = TestThreads(
                threadCount,
                startCount,
                finCount,
                scaler,
                id => {
                    while (ThreadSafeArrayStates.LockFailed == lda.TryAdd(id, 1000)) {
                    }
                }
            );


            Assert.AreEqual(threadCount * scaler, lda.Count);

            for (var i = 0; i < lda.Count; i++) {
                Assert.AreEqual(ThreadSafeArrayStates.Success, lda.TryGet(i, out var v));
                _counts[v]++;
            }

            foreach (var kv in _counts)
                Assert.AreEqual(scaler, kv.Value);

            Console.WriteLine(lda.LockFails);
        }

        private static void TestAdd(ThreadSafeDynamicArray<int> lda, int expectedValue, int expectedSize) {
            Assert.AreEqual(ThreadSafeArrayStates.Success, lda.TryAdd(expectedValue));
            Assert.AreEqual(expectedSize, lda.Count);
            Assert.AreEqual(ThreadSafeArrayStates.Success, lda.TryGet(expectedSize - 1, out var actual));
            Assert.AreEqual(expectedValue, actual);
        }

        private static void TestRemove(ThreadSafeDynamicArray<int> lda,
            int position,
            int expectedSize,
            int?expectedNewValue) {
            Assert.AreEqual(ThreadSafeArrayStates.Success, lda.TryRemove(position));
            Assert.AreEqual(expectedSize, lda.Count);
            if (!expectedNewValue.HasValue)
                return;

            Assert.AreEqual(ThreadSafeArrayStates.Success, lda.TryGet(0, out var actual));
            Assert.AreEqual(expectedNewValue.Value, actual);
        }
    }
}