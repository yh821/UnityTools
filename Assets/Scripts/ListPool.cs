using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : new()
{
    private readonly Stack<T> mStack = new Stack<T>();
    private readonly Action<T> mOnRelease;

    public ObjectPool(Action<T> onRelease)
    {
        mOnRelease = onRelease;
    }

    public T Get()
    {
        var t = mStack.Count==0 ? new T() : mStack.Pop();
        return t;
    }

    public void Release(T t){
        if(mStack.Count>0 && ReferenceEquals(mStack.Peek(),t))
            Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
        mOnRelease(t);
        mStack.Push(t);
    }

}