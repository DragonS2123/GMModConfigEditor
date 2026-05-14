using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace GMCraftTableEditor.Services;

/// <summary>
/// Debounce-таймер для автосохранения.
/// При вызове MarkDirty() запускает (или перезапускает) таймер на N секунд.
/// По истечении вызывает колбэк сохранения.
/// </summary>
public class AutoSaveService
{
    private readonly DispatcherTimer _timer;
    private readonly Action _onSave;
    private readonly Action<bool> _onDirtyChanged; // true = есть несохранённые изменения

    public bool IsDirty { get; private set; }

    public AutoSaveService(Action onSave, Action<bool> onDirtyChanged, double delaySeconds = 4.0)
    {
        _onSave         = onSave;
        _onDirtyChanged = onDirtyChanged;

        _timer          = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(delaySeconds)
        };
        _timer.Tick += (_, _) => SaveNow();
    }

    /// <summary>Отметить что есть несохранённые изменения и запустить таймер.</summary>
    public void MarkDirty()
    {
        _timer.Stop();

        if (!IsDirty)
        {
            IsDirty = true;
            _onDirtyChanged(true);
        }

        _timer.Start();
    }

    /// <summary>Немедленно сохранить и сбросить флаг.</summary>
    public void SaveNow()
    {
        _timer.Stop();
        if (!IsDirty) return;
        try
        {
            _onSave();
        }
        catch { /* не крашим при автосохранении */ }
        IsDirty = false;
        _onDirtyChanged(false);
    }

    /// <summary>Сбросить флаг без сохранения (например, после ручного Save).</summary>
    public void MarkClean()
    {
        _timer.Stop();
        IsDirty = false;
        _onDirtyChanged(false);
    }
}

/// <summary>
/// Менеджер автосохранения для всех открытых конфигов.
/// Каждый конфиг регистрируется под своим ключом.
/// </summary>
public class AutoSaveManager
{
    private readonly Dictionary<string, AutoSaveService> _services = new();
    private readonly Action<string, bool> _onDirtyChanged;

    public AutoSaveManager(Action<string, bool> onDirtyChanged)
    {
        _onDirtyChanged = onDirtyChanged;
    }

    public void Register(string key, Action saveAction)
    {
        if (_services.ContainsKey(key)) return;
        _services[key] = new AutoSaveService(saveAction, dirty => _onDirtyChanged(key, dirty));
    }

    public void Unregister(string key)
        => _services.Remove(key);

    public void MarkDirty(string key)
    {
        if (_services.TryGetValue(key, out var svc)) svc.MarkDirty();
    }

    public void MarkClean(string key)
    {
        if (_services.TryGetValue(key, out var svc)) svc.MarkClean();
    }

    public void SaveAll()
    {
        foreach (var svc in _services.Values) svc.SaveNow();
    }

    public bool AnyDirty => _services.Values.Any(s => s.IsDirty);
}
