using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LlamaTest : MonoBehaviour
{
    private static LlamaTest _instance;
    
#if UNITY_EDITOR_OSX
    private const string kLibName = "libllamacpp-wrapper";
#else
    private const string kLibName = "__Internal";
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CompletionCallback(IntPtr resultPtr);

    [DllImport(kLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr create_instance(string path);

    [DllImport(kLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void llama_complete(IntPtr instance, string text, IntPtr completion);

    [MonoPInvokeCallback(typeof(Action<string>))]
    public static void OnNativeCallback(string result)
    {
        Debug.Log(result);
    }

    [SerializeField] private string _modelFilePath = "models/llama-2-7b-chat.Q4_K_M.gguf";
    [SerializeField] private TMP_Text _result;
    [SerializeField] private TMP_InputField _prompt;
    [SerializeField] private Button _sendButton;

    private CompletionCallback _completionCallback;
    private GCHandle _gcHandle;
    private IntPtr _llamaInstance;

    private bool _hasResult = false;
    private IntPtr _resultPtr;

    private void Start()
    {
        _instance = this;
        
        string filepath = $"{Application.streamingAssetsPath}/{_modelFilePath}";
        _llamaInstance = create_instance(filepath);

        _sendButton.onClick.AddListener(StartPredict);
    }

    private void Update()
    {
        if (_hasResult)
        {
            ShowResult();
        }
    }

    private void StartPredict()
    {
        Debug.Log($"1: {Thread.CurrentThread.ManagedThreadId}");

        Task.Run(Predict);
    }

    private void Predict()
    {
        Debug.Log($"2: {Thread.CurrentThread.ManagedThreadId}");

        _completionCallback = StaticCallback;
        IntPtr completionPtr = Marshal.GetFunctionPointerForDelegate(_completionCallback);
        _gcHandle = GCHandle.Alloc(_completionCallback);

        llama_complete(_llamaInstance, _prompt.text, completionPtr);
    }

    private void ShowResult()
    {
        _hasResult = false;

        if (_resultPtr == IntPtr.Zero) return;

        string result = Marshal.PtrToStringAnsi(_resultPtr);

        Debug.Log(result);

        _result.text = result;

        Marshal.FreeHGlobal(_resultPtr);

        _resultPtr = IntPtr.Zero;

        _gcHandle.Free();
    }

    private void Callback(IntPtr resultPtr)
    {
        _hasResult = true;
        _resultPtr = resultPtr;
    }

    [MonoPInvokeCallback(typeof(CompletionCallback))]
    private static void StaticCallback(IntPtr resultPtr)
    {
        _instance.Callback(resultPtr);
    }
}