using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// used inside BackgroundCoroutine job to forcefully stop background coroutine execution until next frame.
/// may be helpful if the next iteration of the coroutine will take a lot of time and we don't want to execute it in the same frame because BackgroundCoroutine think there is enough time for one more iteration
/// also can be used in regular coroutines, 'yield return Progress' is equivalent to 'yield return null'
/// </summary>
public class WaitForNextFrame : CustomYieldInstruction
{ public override bool keepWaiting { get { return false; } } }



// Frame timeline:                      Time --->
//                 | CurrentFrame (CPU)                      | Next frame (CPU)
//                 |                                         |
// Main Thread:    |-[Updates][DrawObjects]--xxxxxxxxxxxxxxx-|-[Updates][DrawObjects]--
//                 |                                         |
// Render Thread:  |----------------[Generate CommandBuffer]-|----------------[Generate
//                 |                                         |
// GPU:            |-------------------------[Render the frame based on a CommandBuffer]
//                 |                                                                    ^
//                                     Here the frame will be actually displayed -------+
//
// '---' means idle time
// 'xxx' means Main Thread idle time where some background work may be performed,
// the BackgroundCoroutine performs the job exactly here and do several iterations (depending on
// the time available before the next frame)

/// <summary>
/// use it to perfrom some background computations in the end of frame free time. It's a wrapper for a
/// regular coroutine, but here multiple IEnumerator.MoveNext() may be perfromed in one frame if there is
/// free time at the end of frame
/// </summary>
public class BackgroundCoroutine : CustomYieldInstruction
{
    public enum Status
    {
        InProgress,
        Paused,
        Cancelled,
        Done
    }

    public event Action OnJobFinished;
    public event Action OnJobCancelled;
    public event Action OnJobPaused;
    public event Action OnJobResumed;

    /// <summary>
    /// Coroutine was started on this MB
    /// </summary>
    public MonoBehaviour monoBehaviour;
    /// <summary>
    /// IEnumerator of the job (automatically MoveNext multiple times each frame)
    /// </summary>
    public IEnumerator job;
    /// <summary>
    /// Time since frame start to pause job execution until the next frame
    /// </summary>
    public float maxFrameTime;
    /// <summary>
    /// Guaranteed workload time within one frame
    /// </summary>
    public float minFrameLoadTime;

    private Coroutine loopCoroutine;
    private Stack<IEnumerator> delayedJobs;

    public Status status { get; private set; }

    public override bool keepWaiting { get { return status != Status.Done & status != Status.Cancelled; } }

    public BackgroundCoroutine(IEnumerator job, float maxFrameTime = 0.012f, float minFrameLoadTime = 0f)
    {
        status = Status.Paused;
        this.maxFrameTime = maxFrameTime;
        this.minFrameLoadTime = minFrameLoadTime;
        this.job = job;
    }

    /// <summary>
    /// Start background job on the MB specified
    /// </summary>
    /// <param name="mb">the MB to start Coroutine on</param>
    public void Start(MonoBehaviour mb)
    {
        status = Status.InProgress;
        monoBehaviour = mb;
        delayedJobs = new Stack<IEnumerator>();
        loopCoroutine = mb.StartCoroutine(BackgroundCoroutineLoop());
    }

    /// <summary>
    /// Job execution will be paused
    /// </summary>
    public void Pause()
    {
        if (status != Status.InProgress)
            return;
        status = Status.Paused;
        if (OnJobPaused != null)
            OnJobPaused();
    }

    /// <summary>
    /// Job execution will be unpaused
    /// </summary>
    public void Resume()
    {
        if (status != Status.Paused)
            return;
        status = Status.InProgress;
        if (OnJobResumed != null)
            OnJobResumed();
    }

    /// <summary>
    /// Stop coroutine without finishing the job
    /// </summary>
    public void Cancel()
    {
        if (status == Status.Done | status == Status.Cancelled)
            return;
        status = Status.Cancelled;
        delayedJobs = null;
        monoBehaviour.StopCoroutine(loopCoroutine);
        if (OnJobCancelled != null)
            OnJobCancelled();
    }

    /// <summary>
    /// Finish the job synchroniously and stom coroutine
    /// </summary>
    public void Finish()
    {
        if (status == Status.Done | status == Status.Cancelled)
            return;
        if (delayedJobs != null)
        {
            status = Status.InProgress;
            UnityEngine.Profiling.Profiler.BeginSample("BackCoroutines Sync Finish");
            monoBehaviour.StopCoroutine(loopCoroutine);
            do
            {
                while (job.MoveNext())
                {
                    var current = job.Current;
#if UNITY_EDITOR
                    if (current != null && current is AsyncOperation || current is ResourceRequest)
                        Debug.LogWarningFormat("Awaiting for '{0}' operation in sync mode", current.GetType().Name);
#endif
                    var en = current as IEnumerator;
                    if (en != null)
                    {
                        delayedJobs.Push(job);
                        job = en;
                    }
                }
                if (delayedJobs.Count > 0)
                    job = delayedJobs.Pop();
                else
                    delayedJobs = null;
            } while (delayedJobs != null);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        status = Status.Done;
        if (OnJobFinished != null)
            OnJobFinished();
    }

    /// <summary>
    /// Use this IEnumerator in case you want other coroutine to continue only when the job will be finished
    /// </summary>
    public IEnumerator Await()
    {
        while (status != Status.Done & status != Status.Cancelled)
            yield return null;
    }

    private IEnumerator BackgroundCoroutineLoop()
    {
        var waiter = new WaitForEndOfFrame();
        yield return waiter;

        float frameStartTime = Time.unscaledTime;
        float workStartTime = Time.realtimeSinceStartup;
        do
        {
            while (true)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Background Iteration");
                var contKey = job.MoveNext();
                UnityEngine.Profiling.Profiler.EndSample();

                if (!contKey) break;
                var current = job.Current;
                if (current != null
                    && !(current is WaitForEndOfFrame))
                {
                    var wfnf = current as WaitForNextFrame;
                    if (wfnf != null)
                    {
                        yield return waiter;
                        workStartTime = Time.realtimeSinceStartup;
                        frameStartTime = Time.unscaledTime;
                    }
                    else
                    {
#if UNITY_EDITOR
                        if (current is Coroutine)
                        {
                            Debug.LogErrorFormat("Trying to invoke a coroutine inside background coroutine\nThe coroutine will be executed in sequential mode");
                        }
#endif
                        var en = current as IEnumerator;
                        if (en != null)
                        {
                            delayedJobs.Push(job);
                            job = en;
                        }
                        else
                            yield return current;
                    }
                }
                float realTime = Time.realtimeSinceStartup;
                while (status == Status.Paused || (realTime - frameStartTime > maxFrameTime
                        && realTime - workStartTime > minFrameLoadTime))
                {
                    yield return waiter;
                    if (job == null)
                        break;
                    workStartTime = Time.realtimeSinceStartup;
                    frameStartTime = Time.unscaledTime;
                }
            }
            if (delayedJobs.Count > 0)
                job = delayedJobs.Pop();
            else
                delayedJobs = null;
        } while (delayedJobs != null);
        Finish();
    }
}

public static class BackgroundCoroutineExtension
{
    /// <summary>
    /// Perform a job as a background coroutine on frame-end free time with maximum lag-free iterations count
    /// </summary>
    /// <param name="mb">MonoBehaviour to start coroutine on</param>
    /// <param name="job">Job to perform in background as IEnumerator</param>
    /// <param name="maxFrameTime">Time since frame start to pause job execution until the next frame</param>
    /// <param name="minFrameLoadTime">Guaranteed workload time within one frame</param>
    /// <returns>Object which has information about job status and allows to control the job execution</returns>
    public static BackgroundCoroutine StartCoroutineInBackground(this MonoBehaviour mb, IEnumerator job, float maxFrameTime = 0.012f, float minFrameLoadTime = 0.0f)
    {
        var bc = new BackgroundCoroutine(job, maxFrameTime, minFrameLoadTime);
        bc.Start(mb);
        return bc;
    }
}
