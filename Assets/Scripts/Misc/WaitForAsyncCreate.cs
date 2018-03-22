using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitForAsyncCreate<T> : CustomYieldInstruction
{
    public T result { get; private set; }
    
    public string error { get; private set; }
    public bool isDone { get; private set; }
    public override bool keepWaiting { get { return !isDone; } }

    public void Finish(T p, string error = null) { result = p; isDone = true; this.error = error; }
}
