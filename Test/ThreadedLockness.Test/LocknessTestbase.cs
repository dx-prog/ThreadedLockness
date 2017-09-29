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
using System.Threading;
using ThreadedLockness.Threading;

namespace ThreadedLockness.Test {
    public class LocknessTestbase {
        protected static Dictionary<long, int> TestThreads(
            int threadCount,
            AtomicCounter startCount,
            AtomicCounter finCount,
            int scaler,
            Action<int> work) {
            var counts = new Dictionary<long, int>();
            var threads = new List<Thread>();
            for (var i = 0; i < threadCount; i++) {
                var thread = new Thread(() => {
                    var id = startCount.Increment();
                    lock (counts) {
                        counts[id] = 0;
                    }
                    while (startCount.TransientValue + 1 < threadCount)
                        Thread.Yield();
                    for (var z = 0; z < scaler; z++)
                        work((int) id);

                    finCount.Increment();
                });
                thread.Start();
                threads.Add(thread);
            }

            while (threads.Any(t => { return t.IsAlive; }))
                Thread.Sleep(1);

            return counts;
        }
    }
}