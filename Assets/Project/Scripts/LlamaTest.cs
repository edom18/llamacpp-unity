using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LlamaTest : MonoBehaviour
{
#if UNITY_EDITOR_OSX
    private const string kLibName = "libllamacpp-wrapper";
#else
    private const string kLibName = "__Internal";
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CompletionCallback(int result);

    [DllImport(kLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr create_instance(string path);

    [DllImport(kLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr llama_complete(IntPtr instance, string text, IntPtr completion);

    [DllImport(kLibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int llama_test(IntPtr instance, string text, IntPtr completion);

    [SerializeField] private string _modelFilePath = "Models/llama-2-7b-chat.Q4_K_M.gguf";
    [SerializeField] private TMP_Text _result;
    [SerializeField] private TMP_InputField _prompt;
    [SerializeField] private Button _sendButton;

    private CompletionCallback _completionCallback;
    private GCHandle _gcHandle;
    private IntPtr _instance;

    private bool _hasResult = false;
    private int _resultValue;

    private void Start()
    {
        string filepath = $"{Application.streamingAssetsPath}/{_modelFilePath}";
        _instance = create_instance(filepath);

        _sendButton.onClick.AddListener(() =>
        {
            _completionCallback = Callback;
            IntPtr completionPtr = Marshal.GetFunctionPointerForDelegate(_completionCallback);
            _gcHandle = GCHandle.Alloc(_completionCallback);

            IntPtr resultPtr = llama_complete(_instance, _prompt.text, completionPtr);
            string result = Marshal.PtrToStringAnsi(resultPtr);
            // int result = llama_test(wrapperPtr, _prompt.text, completionPtr);
            Debug.Log(result);
        });
    }

    private void Update()
    {
        if (_hasResult)
        {
            ShowResult();
        }
    }

    private void ShowResult()
    {
        _hasResult = false;
        
        Debug.Log(_resultValue);

        _result.text = _resultValue.ToString();

        _resultValue = 0;

        _gcHandle.Free();
    }

    private void Callback(int result)
    {
        _hasResult = true;
        _resultValue = result;
    }
}