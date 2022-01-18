using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 改变游戏窗口的风格、大小、层级
/// </summary>
public class WindowTool : MonoBehaviour
{
	public static WindowTool Instance;
	private void Awake()
	{
		Instance = this;
		Instance.Init();
	}

    #region Win32Api

    [DllImport("User32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hPos, int x, int y, int cx, int cy, uint nflags);
    [DllImport("User32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("User32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("User32.dll")]
    private static extern IntPtr GetSystemMetrics(int nIndex);

    #endregion

    /// <summary>
    /// 窗口风格
    /// </summary>
    public class WindowStyle
    {
        public const uint WS_BORDER = 0x00800000,
        WS_CAPTION = 0x00C00000,
        WS_CHILD = 0x40000000,
        WS_CHILDWINDOW = 0x40000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_DISABLED = 0x08000000,
        WS_DLGFRAME = 0x00400000,
        WS_GROUP = 0x00020000,
        WS_HSCROLL = 0x00100000,
        WS_ICONIC = 0x20000000,
        WS_MAXIMIZE = 0x01000000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_MINIMIZE = 0x20000000,
        WS_MINIMIZEBOX = 0x00020000,
        WS_OVERLAPPED = 0x00000000,
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_POPUP = 0x80000000,
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        WS_SIZEBOX = 0x00040000,
        WS_SYSMENU = 0x00080000,
        WS_TABSTOP = 0x00010000,
        WS_THICKFRAME = 0x00040000,
        WS_TILED = 0x00000000,
        WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_VISIBLE = 0x10000000,
        WS_VSCROLL = 0x00200000;
    }

    /// <summary>
    /// 窗口扩展风格
    /// </summary>
    public class WindowStyleEx
    {
        public const uint WS_EX_ACCEPTFILES = 0x00000010,
        WS_EX_APPWINDOW = 0x00040000,
        WS_EX_CLIENTEDGE = 0x00000200,
        WS_EX_COMPOSITED = 0x02000000,
        WS_EX_CONTEXTHELP = 0x00000400,
        WS_EX_CONTROLPARENT = 0x00010000,
        WS_EX_DLGMODALFRAME = 0x00000001,
        WS_EX_LAYERED = 0x00080000,
        WS_EX_LAYOUTRTL = 0x00400000,
        WS_EX_LEFT = 0x00000000,
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        WS_EX_LTRREADING = 0x00000000,
        WS_EX_MDICHILD = 0x00000040,
        WS_EX_NOACTIVATE = 0x08000000,
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        WS_EX_RIGHT = 0x00001000,
        WS_EX_RIGHTSCROLLBAR = 0x00000000,
        WS_EX_RTLREADING = 0x00002000,
        WS_EX_STATICEDGE = 0x00020000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_TOPMOST = 0x00000008,
        WS_EX_TRANSPARENT = 0x00000020,
        WS_EX_WINDOWEDGE = 0x00000100;
    }

    private const int GWL_STYLE = -16;//表示与他相关的参数是窗口风格
    private const int GWL_EXSTYLE = -20;//表示与他相关的参数是窗口扩展风格

    //表示用于Win32Api的SetWindowPos方法的某个参数
    private const uint SWP_NOSIZE = 0x0001;//表示此次设置不改变大小
    private const uint SWP_NOMOVE = 0x0002;//表示此次设置不改变位置
    private const uint SWP_NOZORDER = 0x0004;//表示此次设置不改变ZOrder
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>
    /// 窗口类型
    /// </summary>
    public enum WindowType
    {
        ExclusiveFullScreen,//独占全屏
        FullScreenWindow,//窗口全屏
        ResizableWindow,//普通可调节大小的窗口
        FixedSizeWindow,//固定大小的窗口
    }

    /// <summary>
    /// 窗口的Z排序设置。
    /// </summary>
    public enum ZOrder
    {
        /// <summary>
        /// 让窗口变为当时的最顶层，相当于给窗口设置了一个"置顶"标志，
        /// 与其他有这个标志的窗口竞争最顶层的位置（鼠标点击可切换哪个窗口成为当时的最顶层），
        /// 所有带这个标志的窗口处在所有不带这个标志的窗口的上面，离用户更近。
        /// </summary>
        TopMost = -1,

        /// <summary>
        /// 取消窗口的"置顶"标志，于是这个窗口就变成了普通窗口，置顶窗口们就不和它一起玩了，它之后便和其他普通窗口一桌竞争了。
        /// 这个设置只对本来就是置顶窗口的窗口有用，对普通窗口没效果。
        /// </summary>
        NoTopMost = -2,

        /// <summary>
        /// 将窗口移动到普通窗口的顶部，依然处在置顶窗口们的下面，依然是普通窗口，不会一直待在顶部，会在以后鼠标点来点去的时候跑到其他窗口下面。
        /// </summary>
        Top = 0,

        /// <summary>
        /// 将窗口移动到普通窗口的底部。其他与Top同理。
        /// </summary>
        Bottom = 1,
    }

    private IntPtr _hWndSelf = new IntPtr(0);//自己的窗口句柄
    public IntPtr hWndSelf
    {
        get
        {
#if UNITY_EDITOR

#elif UNITY_STANDALONE_WIN

            if (_hWndSelf.ToInt32() == 0)
            {
                Debug.Log("窗口句柄为0，无法操作窗口。");
            }

#endif
            return _hWndSelf;
        }
    }

    public void Init()
    {
#if UNITY_EDITOR
        _hWndSelf = new IntPtr(0);//在编辑器里面，让窗口句柄为0，这样就相当于没有指定窗口，调用Win32Api就不会有任何效果。
#elif UNITY_STANDALONE_WIN
        _hWndSelf = FindWindow(null, Application.productName);//打Windows包之后才有效果
#endif
    }

    public void SetWindow(int width, int height, WindowType windowType, ZOrder zOrder = ZOrder.NoTopMost)
    {
        switch (windowType)
        {
            case WindowType.ExclusiveFullScreen:
                //独占全屏的时候，其他TopMost的窗口无法出现在它上面
                Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                break;

            case WindowType.FullScreenWindow:
                Instance.StartCoroutine(SetFullScreenWindow(width, height));
                break;

            case WindowType.ResizableWindow:
                Instance.StartCoroutine(SetResizableWindow(width, height, zOrder));
                break;

            case WindowType.FixedSizeWindow:
                Instance.StartCoroutine(SetFixedSizeWindow(width, height, zOrder));
                break;

            default:
                break;
        }
    }


    /// <summary>
    /// 设置为窗口全屏模式。在做实验的时候，发现有时从FullScreenMode.ExclusiveFullScreen转到FullScreenMode.FullScreenWindow(比如从720P到更高分辨率)
    /// 要两次才能成功（第一次没有完全成功）（可能与Screen.SetResolution的效果实际执行时刻有关？），所以使用协程执行两次。
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private IEnumerator SetFullScreenWindow(int width, int height)
    {
        //好像FullScreenMode.FullScreenWindow这种模式ZOrder默认就是TopMost，不用改。
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        yield return new WaitUntil(() => { return Screen.fullScreenMode == FullScreenMode.FullScreenWindow; });
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
    }

    /// <summary>
    /// 设置为可调整大小的窗口模式
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private IEnumerator SetResizableWindow(int width, int height, ZOrder zOrder = ZOrder.NoTopMost)
    {
        //SetWindowLong(hWndSelf, GWL_STYLE, (int)WindowStyle.WS_OVERLAPPEDWINDOW);
        //SetSizeAndZOrder(width, height, zOrder);
        //直接使用SetWindowLong进行设置可能并不准，因为Unity做的可能比我想象的更多，
        //使用Screen.SetResolution进行设置的时候，可能Unity做了很多操作，
        //比如会劫持某些窗口消息（比如能控制窗口大小变化的消息），而直接使用SetWindowLong可能无法消除Unity的干扰，
        //于是可以像下面一样先使用Screen.SetResolution方法，再使用SetWindowLong方法添加可调节大小的属性（或者减少某些属性）。

        //这样操作默认是设置为固定大小、不可调节大小的窗口
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        yield return new WaitUntil(() => { return Screen.fullScreenMode == FullScreenMode.Windowed; });
        //为窗口风格添加可调节大小以及激活最大化按钮的风格
        SetWindowLong(hWndSelf, GWL_STYLE, (int)(GetWindowLong(hWndSelf, GWL_STYLE) | WindowStyle.WS_SIZEBOX | WindowStyle.WS_MAXIMIZEBOX));
        SetZOrder(zOrder);
    }

    /// <summary>
    /// 设置为固定尺寸，不可调整大小的窗口模式
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private IEnumerator SetFixedSizeWindow(int width, int height, ZOrder zOrder = ZOrder.NoTopMost)
    {
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        yield return new WaitUntil(() => { return Screen.fullScreenMode == FullScreenMode.Windowed; });
        //为窗口风格删除可调节大小以及激活最大化按钮的风格
        SetWindowLong(hWndSelf, GWL_STYLE, (int)(GetWindowLong(hWndSelf, GWL_STYLE) & ~WindowStyle.WS_SIZEBOX & ~WindowStyle.WS_MAXIMIZEBOX));
        SetZOrder(zOrder);
    }

    public void SetSizeAndZOrder(int width, int height, ZOrder zOrder)
    {
        SetWindowPos(hWndSelf, new IntPtr((int)zOrder), 0, 0, width, height, SWP_NOMOVE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    }

    /// <summary>
    /// 设置的是整个窗口的大小。而非客户区（也就是游戏画面）的尺寸的大小。
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void SetSize(int width, int height)
    {
        SetWindowPos(hWndSelf, IntPtr.Zero, 0, 0, width, height, SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    }

    public void SetZOrder(ZOrder zOrder)
    {
        SetWindowPos(hWndSelf, new IntPtr((int)zOrder), 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    }

    /// <summary>
    /// 设置为置顶窗口
    /// </summary>
    public void SetTopMost()
    {
        SetZOrder(ZOrder.TopMost);
    }

    /// <summary>
    /// 取消窗口置顶
    /// </summary>
    public void CancelTopMost()
    {
        SetZOrder(ZOrder.NoTopMost);
    }

}