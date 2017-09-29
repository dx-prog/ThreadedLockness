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

namespace ThreadedLockness.Threading {
    public class AutoLock<T> : IDisposable {
        private readonly IFreeLock<T> _lockObj;
        private readonly string _owner;
        private string _caller;
        private T _token;

        public AutoLock(IFreeLock<T> lockObj, string owner) {
            _lockObj = lockObj;
            _owner = owner;
        }

        public bool?Success { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public virtual void Dispose() {
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                try {
                    if (Success == true)
                        _lockObj.ReleaseLock(_token);
                }
                catch (Exception ex) {
                    throw new Exception(_owner + "/" + _caller, ex);
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public virtual bool Initialize(int spinCount = 100, [CallerMemberName] string caller = null) {
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally {
                do {
                    if (Success.HasValue)
                        throw new InvalidOperationException("unexpected operational state");

                    if (_lockObj.Debug)
                        Console.WriteLine("BEFORE CONFIRM LOCK {0}:{1}", caller, _token);

                    if (!_lockObj.AcquireLock(out _token)) {
                        if (Equals(_token, default(T)))
                            throw new InvalidOperationException("unexpected state change");

                        continue;
                    }

                    if (Success != null)
                        throw new InvalidOperationException("one time use object");

                    if (_lockObj.Debug)
                        Console.WriteLine("CONFIRM LOCK:{0}:{1}", caller, _token);
                    Success = true;
                    _caller = caller;
                    break;
                } while (spinCount-- > 0);
            }

            return Success.GetValueOrDefault();
        }
    }
}