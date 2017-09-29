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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadedLockness.Collections;

namespace ThreadedLockness.Test {
    [TestClass]
    public class ThreadSafeLinkListTests : LocknessTestbase {
        [TestMethod]
        public void ThreadSafeLinkList_AddLast() {
            var list = new ThreadSafeLinkedList<int>();

            TestAddLast(list, 10);
            Assert.AreEqual(list.Last, list.First);
            TestAddLast(list, 11);
            TestAddLast(list, 12);
            TestAddLast(list, 13);
            Assert.AreEqual(4, list.Count);
        }

        [TestMethod]
        public void ThreadSafeLinkList_AddFirst() {
            var list = new ThreadSafeLinkedList<int>();

            TestAddFirst(list, 10);
            Assert.AreEqual(list.Last, list.First);
            TestAddFirst(list, 11);
            TestAddFirst(list, 12);
            TestAddFirst(list, 13);
            Assert.AreEqual(4, list.Count);
        }

        [TestMethod]
        public void ThreadSafeLinkList_AddAfter_InOrder() {
            var list = new ThreadSafeLinkedList<int>();


            TestAddAfter(list, list.TryGetAt(-1), 10);
            TestAddAfter(list, list.TryGetAt(-1), 11);
            TestAddAfter(list, list.TryGetAt(-1), 12);
            TestAddAfter(list, list.TryGetAt(-1), 13);
            Assert.AreEqual(4, list.Count);
        }

        [TestMethod]
        public void ThreadSafeLinkList_AddAfter_Random() {
            var list = new ThreadSafeLinkedList<int>();

            var random = new Random((int) DateTime.Now.Ticks);
            for (var i = 0; i < 100; i++) {
                var pos = random.Next(-1, (int) list.Count);
                TestAddAfter(list, list.TryGetAt(pos), i);
            }

            Assert.AreEqual(100, list.Count);
            Assert.AreEqual(100, list.Select(x=>x.Value).Distinct().Count());
        }

        [TestMethod]
        public void ThreadSafeLinkList_AddBefore_Random()
        {
            var list = new ThreadSafeLinkedList<int>();

            var random = new Random((int)DateTime.Now.Ticks);
            for (var i = 0; i < 100; i++)
            {
                var pos = random.Next(-1, (int)list.Count);
                TestAddBefore(list, list.TryGetAt(pos), i);
            }

            Assert.AreEqual(100, list.Count);
            Assert.AreEqual(100, list.Select(x => x.Value).Distinct().Count());
        }
        private static void TestAddLast(ThreadSafeLinkedList<int> list, int v) {
            list.TryAddLast(v, out var n);
            Assert.AreEqual(v, list.Last.Value);
            Assert.AreEqual(v, n.Value);
            Assert.AreEqual(list.Last, n);
        }

        private static void TestAddFirst(ThreadSafeLinkedList<int> list, int v) {
            list.TryAddFirst(v, out var n);
            Assert.AreEqual(v, list.First.Value);
            Assert.AreEqual(v, n.Value);
            Assert.AreEqual(list.First, n);
        }

        private static void TestAddBefore(ThreadSafeLinkedList<int> list,
            ThreadSafeLinkedList<int>.ThreadSafeLinkListNode node,
            int v) {
            list.TryAddBefore(node, v, out var n);
            Assert.AreEqual(v, n.Value);
        }

        private static void TestAddAfter(ThreadSafeLinkedList<int> list,
            ThreadSafeLinkedList<int>.ThreadSafeLinkListNode node,
            int v) {
            list.TryAddAfter(node, v, out var n);
            Assert.AreEqual(v, n.Value);
        }
    }
}