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
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadedLockness.Collections;
using ThreadedLockness.Threading;

namespace ThreadedLockness.Test {
    [TestClass]
    public class ThreadSafeHashSetTests : LocknessTestbase {

        [TestMethod]
        public void LocknessSet_ThreadedAdd_20x1000() {
            var set = new ThreadSafeHashSet<int>(256);
            var startCount = new AtomicCounter();
            var finCount = new AtomicCounter();
            var sw = new Stopwatch();
            for (var i = 0; i < 10; i++) {
                sw.Start();
                TestThreadsBaseLine(20, startCount, finCount, 1000);
                sw.Stop();
                Console.WriteLine("Base line {0}", sw.Elapsed);
            }
            //warm up
            TestThreads(2, startCount, set, finCount, 1000);
            for (var i = 0; i < 10; i++) {
                set = new ThreadSafeHashSet<int>(64);
                sw.Restart();
                var expected = TestThreads(20, startCount, set, finCount, 100);
                sw.Stop();
                Console.WriteLine("Test {0}:  {1}", i, sw.Elapsed);
                var actualSize = set.ForceCount();
                Console.WriteLine("{0} {1} ", actualSize, expected);
                Assert.AreEqual(expected, actualSize);
            }
        }

        private static void TestThreadsBaseLine(int threadCount,
            AtomicCounter startCount,
            AtomicCounter finCount,
            int scaler = 100) {
            var set = new HashSet<long>();
            var uniqueId = new AtomicCounter();
            var counts = TestThreads(
                threadCount,
                startCount,
                finCount,
                scaler,
                id => {
                    var subId = uniqueId.Increment();
                    lock (set) {
                        set.Add(subId);
                    }
                }
            );
        }

        private static int TestThreads(int threadCount,
            AtomicCounter startCount,
            ThreadSafeHashSet<int> lda,
            AtomicCounter finCount,
            int scaler = 100) {
            var uniqueId = new AtomicCounter();
            var counts = TestThreads(
                threadCount,
                startCount,
                finCount,
                scaler,
                id => {
                    var subId = uniqueId.Increment();
                    while (lda.TryAdd((int) subId, 100) == false) {
                    }
                }
            );

            return threadCount * scaler;
        }
    }
}