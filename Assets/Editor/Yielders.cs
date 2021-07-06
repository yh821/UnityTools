﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Comparers;

// Usage:
//    yield return new WaitForEndOfFrame();     =>      yield return Yielders.EndOfFrame;
//    yield return new WaitForFixedUpdate();    =>      yield return Yielders.FixedUpdate;
//    yield return new WaitForSeconds(1.0f);    =>      yield return Yielders.GetWaitForSeconds(1.0f);

// http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/

public static class Yielders
{
    public static bool Enabled = true;

    public static int _internalCounter; // counts how many times the app yields

    // WARNING: 
    //      (Gu Lu) The comments below are incorrect in Unity 5.5.0
    //          - float DOES NOT needs customized IEqualityComparer (but enums and structs does)
    //      however all these lines are kept to help later reader to share this knowledge (for education purpose only). 
    //------------------------------------------------------------------
    ///////////////////// obsoleted code begins \\\\\\\\\\\\\\\\\\\\\\\\
    //
    //// dictionary with a key of ValueType will box the value to perform comparison / hash code calculation while scanning the hashtable.
    //// here we implement IEqualityComparer<float> and pass it to your dictionary to avoid that GC
    //class FloatComparer : IEqualityComparer<float>
    //{
    //    bool IEqualityComparer<float>.Equals(float x, float y)
    //    {
    //        return x == y;
    //    }
    //    int IEqualityComparer<float>.GetHashCode(float obj)
    //    {
    //        return obj.GetHashCode();
    //    }
    //}
    //\\\\\\\\\\\\\\\\\\\\\\\\ obsoleted code ends /////////////////////
    //------------------------------------------------------------------

    static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
    public static WaitForEndOfFrame EndOfFrame
    {
        get { _internalCounter++; return Enabled ? _endOfFrame : new WaitForEndOfFrame(); }
    }

    static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
    public static WaitForFixedUpdate FixedUpdate
    {
        get { _internalCounter++; return Enabled ? _fixedUpdate : new WaitForFixedUpdate(); }
    }

    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        _internalCounter++;

        if (!Enabled)
            return new WaitForSeconds(seconds);

        WaitForSeconds wfs;
        if (!_waitForSecondsYielders.TryGetValue(seconds, out wfs))
            _waitForSecondsYielders.Add(seconds, wfs = new WaitForSeconds(seconds));
        return wfs;
    }

    public static WaitForSeconds GetWaitForSeconds(int seconds)
    {
        _internalCounter++;

        if (!Enabled)
            return new WaitForSeconds(seconds);

        WaitForSeconds wfs;
        if (!_waitForSecondsYielders_int.TryGetValue(seconds, out wfs))
            _waitForSecondsYielders_int.Add(seconds, wfs = new WaitForSeconds(seconds));
        return wfs;
    }

    public static void ClearWaitForSeconds()
    {
        _waitForSecondsYielders.Clear();
        _waitForSecondsYielders_int.Clear();
    }

    static Dictionary<float, WaitForSeconds> _waitForSecondsYielders = new Dictionary<float, WaitForSeconds>(100, new FloatComparer());
    static Dictionary<int, WaitForSeconds> _waitForSecondsYielders_int = new Dictionary<int, WaitForSeconds>(100);
}
