using System.Collections.Frozen;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public class GameTaskListener
{
    private readonly IReloadedHooks _hooks;

    [Function(CallingConventions.Fastcall)]
    public unsafe delegate IntPtr GameTaskOnUpdate(GameTask* gameTask, float arg2, IntPtr arg3, float arg4);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate void GameTaskOnDestroy(GameTask* eventInfo);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate IntPtr CreateGameTaskType(IntPtr param_1, char* objName, byte param_3, byte param_4,
        int param_5, uint param_6, GameTaskOnUpdate* runtimeFunc, long param_8, GameTaskOnDestroy* onDestroyFunc,
        IntPtr args, IntPtr param_11);


    private readonly Dictionary<int, IReverseWrapper<GameTaskOnDestroy>> _temporaryHooks = new();

    private readonly Dictionary<IntPtr, Action> _onCreateListeners = new();
    private readonly Dictionary<IntPtr, Action> _onDestroyListeners = new();

    private readonly Dictionary<IntPtr, GameTaskOnDestroy> originalOnDestroyFuncPtr = new();
    private readonly Dictionary<IntPtr, IntPtr> originalOnDestroyHooks = new();

    private FrozenDictionary<IntPtr, Action> _frozenCreateListeners;
    private FrozenDictionary<IntPtr, Action> _frozenDestroyListeners;

    private IHook<CreateGameTaskType> _createGameTask;
    private readonly IReverseWrapper<GameTaskOnDestroy> _onDestroyWrapperHook;

    public unsafe GameTaskListener(IReloadedHooks hooks)
    {
        _hooks = hooks;

        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 20 48 8B F1 41 8B E9",
            address => _createGameTask =
                hooks.CreateHook<CreateGameTaskType>(CreateGameTaskImpl, address).Activate());

        _onDestroyWrapperHook = hooks.CreateReverseWrapper<GameTaskOnDestroy>(OnDestroyWrapper);
    }

    /**
    * Freezes the listeners for a performance benefit.
    */
    public void FreezeListeners()
    {
        _frozenCreateListeners = _onCreateListeners.ToFrozenDictionary();
        _frozenDestroyListeners = _onDestroyListeners.ToFrozenDictionary();
    }

    public void ListenForTaskCreate(IntPtr runtimeFunc, Action callback)
    {
        if (_frozenCreateListeners is not null)
        {
            throw new InvalidOperationException("Tried to add a listener to the task after the listeners were frozen.");
        }

        if (_onCreateListeners.ContainsKey(runtimeFunc))
        {
            _onCreateListeners[runtimeFunc] += callback;
        }
        else
        {
            _onCreateListeners.Add(runtimeFunc, callback);
        }
    }

    public void ListenForTaskDestroy(IntPtr runtimeFunc, Action callback)
    {
        if (_frozenDestroyListeners is not null)
        {
            throw new InvalidOperationException("Tried to add a listener to the task after the listeners were frozen.");
        }

        if (_onDestroyListeners.ContainsKey(runtimeFunc))
        {
            _onDestroyListeners[runtimeFunc] += callback;
        }
        else
        {
            _onDestroyListeners.Add(runtimeFunc, callback);
        }
    }

    private unsafe void OnDestroyWrapper(GameTask* gameTask)
    {
        if (_frozenDestroyListeners.ContainsKey(gameTask->runtimeFunc))
        {
            _frozenDestroyListeners[gameTask->runtimeFunc].Invoke();
        }

        if (originalOnDestroyFuncPtr.ContainsKey(gameTask->runtimeFunc))
        {
            originalOnDestroyFuncPtr[gameTask->runtimeFunc].Invoke(gameTask);
            originalOnDestroyFuncPtr.Remove(gameTask->runtimeFunc);
            originalOnDestroyHooks.Remove(gameTask->runtimeFunc);
        }
    }

    private unsafe IntPtr CreateGameTaskImpl(IntPtr param_1, char* taskName, byte param_3, byte param_4,
        int param_5, uint param_6, GameTaskOnUpdate* runtimeFunc, long param_8, GameTaskOnDestroy* onDestroyFunc,
        IntPtr args, IntPtr param_11)
    {
        // MyLogger.DebugLog("[CREATE GAME TASK] " + StrTools.CStrToString(taskName));
        
        if (_frozenCreateListeners.ContainsKey((IntPtr)runtimeFunc))
        {
            _frozenCreateListeners[(IntPtr)runtimeFunc].Invoke();
        }

        GameTaskOnDestroy* myOnDestroy;
        if (_frozenDestroyListeners.ContainsKey((IntPtr)runtimeFunc))
        {
            originalOnDestroyFuncPtr[(IntPtr)runtimeFunc] =
                _hooks.CreateWrapper<GameTaskOnDestroy>((IntPtr)onDestroyFunc, out var addr);
            originalOnDestroyHooks.TryAdd((IntPtr)runtimeFunc, addr);
            myOnDestroy = (GameTaskOnDestroy*)_onDestroyWrapperHook.WrapperPointer;
        }
        else
        {
            myOnDestroy = onDestroyFunc;
        }

        return _createGameTask.OriginalFunction(param_1, taskName, param_3, param_4, param_5, param_6, runtimeFunc,
            param_8, myOnDestroy, args, param_11);
    }
}