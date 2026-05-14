using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace GMCraftTableEditor.Services;

/// <summary>
/// Простой стек Undo/Redo для TextBox изменений.
/// Записи добавляются при потере фокуса (LostKeyboardFocus).
/// Ctrl+Z/Y перехватываются в MainWindow.PreviewKeyDown.
/// </summary>
public class UndoRedoManager
{
    private record Entry(TextBox Box, string OldValue, string NewValue);

    private readonly Stack<Entry> _undo = new();
    private readonly Stack<Entry> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Записать изменение в историю.</summary>
    public void RecordChange(TextBox box, string oldValue, string newValue)
    {
        if (oldValue == newValue) return;
        _undo.Push(new Entry(box, oldValue, newValue));
        _redo.Clear();
    }

    /// <summary>Откатить последнее изменение.</summary>
    public void Undo(Action<TextBox>? postApply = null)
    {
        if (!CanUndo) return;
        var e = _undo.Pop();
        _redo.Push(e);
        e.Box.Text = e.OldValue;
        e.Box.Focus();
        e.Box.CaretIndex = e.OldValue.Length;
        postApply?.Invoke(e.Box);
    }

    /// <summary>Повторить отменённое изменение.</summary>
    public void Redo(Action<TextBox>? postApply = null)
    {
        if (!CanRedo) return;
        var e = _redo.Pop();
        _undo.Push(e);
        e.Box.Text = e.NewValue;
        e.Box.Focus();
        e.Box.CaretIndex = e.NewValue.Length;
        postApply?.Invoke(e.Box);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}

/// <summary>
/// Снапшот-стек для отмены операций с коллекциями (удаление, добавление).
/// Сохраняет полный JSON объекта перед операцией.
/// </summary>
public class SnapshotUndoStack
{
    private record SnapshotEntry(string Description, string Json, Action<string> Restore);

    private readonly Stack<SnapshotEntry> _undo = new();
    private readonly Stack<SnapshotEntry> _redo = new();
    private readonly int _maxDepth;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoDescription => _undo.Count > 0 ? _undo.Peek().Description : null;
    public string? RedoDescription => _redo.Count > 0 ? _redo.Peek().Description : null;

    public SnapshotUndoStack(int maxDepth = 50)
    {
        _maxDepth = maxDepth;
    }

    /// <summary>Сохранить снапшот перед операцией.</summary>
    public void Push(string description, string json, Action<string> restore)
    {
        _undo.Push(new SnapshotEntry(description, json, restore));
        _redo.Clear();
        // Ограничиваем глубину
        if (_undo.Count > _maxDepth)
        {
            var temp = _undo.ToArray();
            _undo.Clear();
            foreach (var e in temp.Take(_maxDepth)) _undo.Push(e);
        }
    }

    public string? Undo()
    {
        if (!CanUndo) return null;
        var e = _undo.Pop();
        // Сохраняем текущее состояние в redo (нужно снапшот текущего)
        // Redo восстановит то что было после операции — но для простоты не реализуем
        e.Restore(e.Json);
        return e.Description;
    }

    public void Clear() { _undo.Clear(); _redo.Clear(); }
}

