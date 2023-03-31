using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

public class NewTestScript
{

    [Test]
    public void CreateLinkedTokenSource()
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
        Assert.IsFalse(cancellationTokenSource.IsCancellationRequested);
        Assert.IsFalse(linked.IsCancellationRequested);

        cancellationTokenSource.Cancel();
        Assert.IsTrue(cancellationTokenSource.IsCancellationRequested);
        Assert.IsTrue(linked.IsCancellationRequested);

        cancellationTokenSource = new CancellationTokenSource();
        linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);

        linked.Cancel();
        Assert.IsFalse(cancellationTokenSource.IsCancellationRequested);
        Assert.IsTrue(linked.IsCancellationRequested);


    }

    [Test]
    public void SemaphoreSlimTest()
    {
        SemaphoreSlim semaphore;
        semaphore = new SemaphoreSlim(2, 2);
        Task[] tasks = new Task[10];
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        DateTime startTime = DateTime.Now;

        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            Task task = Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    semaphore.Wait();
                    try
                    {
                        Debug.Log($"{index}: {DateTime.Now.Subtract(startTime).TotalSeconds:0.#}s");
                    }
                    finally
                    {
                        int n2 = semaphore.CurrentCount;
                        int n = semaphore.Release();
                        Debug.Log("Release count: " + n + ", current count: " + semaphore.CurrentCount + ", before current: " + n2);
                    }
                    Thread.Sleep(1000);
                }
                Debug.Log($"{index}: done, {DateTime.Now.Subtract(startTime).TotalSeconds:0.#}s");
            });
            tasks[i] = task;
        }


        Thread.Sleep(2000);

        Debug.Log($"Cancel all, {DateTime.Now.Subtract(startTime).TotalSeconds:0.#}s");
        cancellationTokenSource.Cancel();
        Task.WaitAll(tasks);

    }

    [Test]
    public void AutoResetEventTest()
    {

        Task[] tasks = new Task[5];
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        DateTime startTime = DateTime.Now;
        int n = 0;
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            Task task = Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    resetEvent.WaitOne();
                    Interlocked.Increment(ref n);
                    Debug.Log($"{index}: {DateTime.Now.Subtract(startTime).TotalSeconds:0.#}s");
                }
                Debug.Log($"{index}: done ======");
            });
            tasks[i] = task;
        }

        Thread.Sleep(10);
        resetEvent.Set();
        Debug.Log("Count: " + 1);
        Assert.AreEqual(1, n);

        resetEvent.Set();
        resetEvent.Set();
        Thread.Sleep(10);
        Debug.Log("Count: " + n);
        Assert.AreEqual(3, n);

        Debug.Log("======= Cancel");
        cancellationTokenSource.Cancel();
        var all = Task.WhenAll(tasks);
        while (!all.IsCompleted)
        {
            resetEvent.Set();
            Thread.Sleep(0);
        }

    }

    [Test]
    public void AutoResetEvent_Timeout()
    {
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        Stopwatch sw = Stopwatch.StartNew();
        int n = 0;
        while (n < 5)
        {
            resetEvent.WaitOne(500);
            n++;
            Debug.Log($"{n} ({sw.Elapsed.TotalSeconds:0.#}s)");
        }
    }

    [Test]
    public void AutoResetEvent_Timeout_Set()
    {
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        Stopwatch sw = Stopwatch.StartNew();
        int n = 0;


        Task.Run(() =>
        {
            while (n < 5)
            {
                resetEvent.Set();
                Thread.Sleep(100);
            }
        });

        while (n < 5)
        {
            resetEvent.WaitOne(500);
            n++;
            Debug.Log($"{n} ({sw.Elapsed.TotalSeconds:0.#}s)");
        }


    }

}
