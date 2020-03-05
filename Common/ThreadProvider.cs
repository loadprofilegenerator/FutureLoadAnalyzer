using System;
using System.Collections.Generic;
using System.Threading;
using Automation;
using JetBrains.Annotations;

namespace Common {
    public class ThreadProvider {
        [CanBeNull] private static ThreadProvider _me;

        private ThreadProvider()
        {
        }

        [NotNull]
        [ItemNotNull]
        public List<Thread> ThreadList { get; } = new List<Thread>();

        public void DisplayRunningThreads()
        {
            int idx = 0;
            lock (ThreadList) {
                foreach (var thread in ThreadList) {
                    if (thread.IsAlive) {
                        Console.WriteLine(idx + ". " + thread.Name + " alive:" + thread.IsAlive);
                        idx++;
                    }
                }
            }
        }

        [NotNull]
        public static ThreadProvider Get()
        {
            if (_me == null) {
                _me = new ThreadProvider();
            }

            return _me;
        }

        [NotNull]
        public Thread MakeThreadAndStart([NotNull] ThreadStart start, [NotNull] string name, bool isStaThread = false)
        {
            var t = new Thread(start);
            t.Name = AutomationUtili.GetCallingMethodAndClass() + " - " + name;
            if (isStaThread) {
                t.SetApartmentState(ApartmentState.STA);
            }

            lock (ThreadList) {
                ThreadList.Add(t);
            }

            t.Start();
            return t;
        }
    }
}