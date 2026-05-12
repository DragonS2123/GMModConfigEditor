using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GMCraftTableEditor.Models;
using GMCraftTableEditor.Services;

namespace GMCraftTableEditor;

// Quest System logic — merged into MainWindow as partial class
public partial class MainWindow
{
    // ─── Quest System State ──────────────────────────────────────────────────

    private GMQuestSystemConfig? _questConfig;
    private string? _questConfigPath;

    private ObservableCollection<QuestFileItem> _quests = new();
    private string? _questFilePath;

    private GMPresetConfig? _presetConfig;
    private string? _presetConfigPath;

    private PlayerQuestData? _playerData;
    private string? _playerDataPath;

    private bool _loading;
    private bool _npcLoading;

// ═════════════════════════════════════════════════════════════════════════
    // GM_QuestSystemCFG
    // ═════════════════════════════════════════════════════════════════════════

    // ── GLOBAL_TYPE ComboBox ──────────────────────────────────────────────────
    private void QN_GlobalType_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        if (sender is ComboBox cb)
            q.GLOBAL_TYPE = cb.SelectedIndex + 1;
    }

    private void LoadGlobalTypeCombo(int value)
    {
        QN_GlobalType.SelectedIndex = value >= 1 && value <= 14 ? value - 1 : 0;
    }

        private void OpenQuestConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "GM_QuestSystemCFG.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _questConfigPath = dlg.FileName;
            _questConfig = JsonService.Load<GMQuestSystemConfig>(_questConfigPath);
            NpcList.ItemsSource = _questConfig.NPC;
            SetStatus($"GM_QuestSystemCFG загружен. NPC: {_questConfig.NPC.Count}");
        }
        catch (Exception ex) { ShowError("Не удалось открыть GM_QuestSystemCFG", ex); }
    }

    private void SaveQuestConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_questConfig == null) { MessageBox.Show("Сначала открой GM_QuestSystemCFG."); return; }
        var path = EnsureSavePath(ref _questConfigPath, "GM_QuestSystemCFG.json");
        if (path == null) return;
        JsonService.Save(path, _questConfig);
        SetStatus($"GM_QuestSystemCFG сохранён: {path}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // id_*.json
    // ═════════════════════════════════════════════════════════════════════════

    private void OpenQuestFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Открыть файл квестов (id_*.json)"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _questFilePath = dlg.FileName;
            _quests = JsonService.Load<ObservableCollection<QuestFileItem>>(_questFilePath);
            NormalizeQuests();
            QuestsList.ItemsSource = _quests;
            SetStatus($"Загружено {_quests.Count} квестов из {System.IO.Path.GetFileName(_questFilePath)}");
        }
        catch (Exception ex) { ShowError("Не удалось открыть файл квестов", ex); }
    }

    private void SaveQuestFile_Click(object sender, RoutedEventArgs e)
    {
        var path = EnsureSavePath(ref _questFilePath, "id_new.json");
        if (path == null) return;
        JsonService.Save(path, _quests);
        SetStatus($"Квесты сохранены: {path}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GM_PRESET_CONFIG
    // ═════════════════════════════════════════════════════════════════════════

    private void OpenPresetConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "GM_PRESET_CONFIG.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _presetConfigPath = dlg.FileName;
            _presetConfig = JsonService.Load<GMPresetConfig>(_presetConfigPath);

            RefreshLootPresetsList();
            TriggersGrid.ItemsSource = _presetConfig.TRIGGER_SETTINGS;
            MappingGrid.ItemsSource  = _presetConfig.MAPPING_SETTINGS;

            SetStatus($"GM_PRESET_CONFIG загружен. Пресетов: {_presetConfig.PRESET_SETTINGS.Count}, триггеров: {_presetConfig.TRIGGER_SETTINGS.Count}");
        }
        catch (Exception ex) { ShowError("Не удалось открыть GM_PRESET_CONFIG", ex); }
    }

    private void SavePresetConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null) { MessageBox.Show("Сначала открой GM_PRESET_CONFIG."); return; }
        var path = EnsureSavePath(ref _presetConfigPath, "GM_PRESET_CONFIG.json");
        if (path == null) return;
        JsonService.Save(path, _presetConfig);
        SetStatus($"GM_PRESET_CONFIG сохранён: {path}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SteamID.json
    // ═════════════════════════════════════════════════════════════════════════

    private void OpenPlayerData_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Открыть файл прогресса игрока (SteamID.json)"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _playerDataPath = dlg.FileName;
            _playerData = JsonService.Load<PlayerQuestData>(_playerDataPath);

            var steamId = System.IO.Path.GetFileNameWithoutExtension(_playerDataPath);
            PlayerSteamIdText.Text     = steamId;
            PlayerReputationText.Text  = _playerData.REPUTATION.ToString("F1");

            InProgressGrid.ItemsSource = _playerData.QUEST_IN_PROGRESS
                .Select(q => new QuestInProgressDisplay(q)).ToList();
            FinishedGrid.ItemsSource   = _playerData.QUEST_IS_FINISH;
            FailedGrid.ItemsSource     = _playerData.QUEST_FAILED;

            SetStatus($"Прогресс: {steamId} | Репутация: {_playerData.REPUTATION} | Выполнено: {_playerData.QUEST_IS_FINISH.Count}");
        }
        catch (Exception ex) { ShowError("Не удалось открыть файл прогресса", ex); }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Квесты — список
    // ═════════════════════════════════════════════════════════════════════════

    private void QuestsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var quest = QuestsList.SelectedItem as QuestFileItem;
        QuestDetailPanel.IsEnabled = quest != null;
        LoadQuestFields(quest);
        RefreshQuestSubGrids(quest);
    }

    private void AddQuest_Click(object sender, RoutedEventArgs e)
    {
        var quest = new QuestFileItem
        {
            ID                   = GetNextQuestId(),
            QUEST_NAME           = "Новый квест",
            RELOAD_QUEST         = -1,
            TIME_LIMIT_FOR_QUEST = -1,
            CAN_CANCEL_QUEST     = 0,
            HOUR_FINISH          = 23,
            MINUTE_FINISH        = 59,
            DAY_QUEST            = new List<string> { "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday" },
            IS_SPECIAL_SETTINGS  = new List<SpecialQuestSettings> { new() }
        };

        _quests.Add(quest);
        QuestsList.SelectedItem = quest;
        QuestsList.ScrollIntoView(quest);
        SetStatus($"Добавлен квест ID={quest.ID}");
    }

    private void DeleteQuest_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem quest) return;

        if (MessageBox.Show(
            $"Удалить квест «{quest.QUEST_NAME}» (ID={quest.ID})?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var _qjson = System.Text.Json.JsonSerializer.Serialize(_quests.ToList());
        var _qname = quest.QUEST_NAME;
        _snapshots.Push($"Удаление квеста «{_qname}»", _qjson, j => {
            var restored = System.Text.Json.JsonSerializer.Deserialize<List<QuestFileItem>>(j)!;
            _quests.Clear();
            foreach (var q in restored) _quests.Add(q);
            QuestsList.ItemsSource = null;
            QuestsList.ItemsSource = _quests;
        });

        _quests.Remove(quest);
        SetStatus($"Квест ID={quest.ID} удалён");
    }

    private void DuplicateQuest_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem original) return;

        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var copy = System.Text.Json.JsonSerializer.Deserialize<QuestFileItem>(json)!;
        copy.ID         = GetNextQuestId();
        copy.QUEST_NAME = original.QUEST_NAME + " (копия)";

        _quests.Add(copy);
        QuestsList.SelectedItem = copy;
        QuestsList.ScrollIntoView(copy);
        SetStatus($"Квест скопирован → ID={copy.ID}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Квесты — детальная панель (поля)
    // ═════════════════════════════════════════════════════════════════════════

    private void LoadQuestFields(QuestFileItem? q)
    {
        _loading = true;
        try
        {
            QN_Name.Text        = q?.QUEST_NAME            ?? "";
            QN_DescShort.Text   = q?.DESCRIPTION_SHORT     ?? "";
            QN_Desc.Text        = q?.DESCRIPTION           ?? "";
            QN_GlobalType.Text  = q?.GLOBAL_TYPE.ToString() ?? "";
            QN_Reload.Text      = q?.RELOAD_QUEST.ToString() ?? "";
            QN_TimeLimit.Text   = q?.TIME_LIMIT_FOR_QUEST.ToString() ?? "";
            QN_CanCancel.Text   = q?.CAN_CANCEL_QUEST.ToString() ?? "";
            QN_Required.Text    = q?.QUEST_REQUIRED        ?? "";
            QN_Unavailable.Text = q?.QUEST_UNAVAILABLE     ?? "";
            QN_RepReward.Text   = q?.REPUTATION_REWARD.ToString()  ?? "";
            QN_RepPenalty.Text  = q?.REPUTATION_PENALTY.ToString() ?? "";
            QN_NeedRep.Text     = q?.NEED_REPUTATION.ToString()    ?? "";
            QN_Unique.Text      = q?.UNIQUE.ToString()     ?? "";
            QN_HStart.Text      = q?.HOUR_START.ToString() ?? "";
            QN_MStart.Text      = q?.MINUTE_START.ToString() ?? "";
            QN_HFinish.Text     = q?.HOUR_FINISH.ToString() ?? "";
            QN_MFinish.Text     = q?.MINUTE_FINISH.ToString() ?? "";

            foreach (var cb in DayQuestPanel.Children.OfType<CheckBox>())
                cb.IsChecked = q?.DAY_QUEST?.Contains(cb.Tag?.ToString() ?? "") ?? false;
        }
        finally { _loading = false; }
    }

    private void QuestField_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _autoSave.MarkDirty("quests");
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        if (sender is not TextBox tb) return;

        switch (tb.Tag?.ToString())
        {
            case "QUEST_NAME":           q.QUEST_NAME = tb.Text; QuestsList.Items.Refresh(); break;
            case "DESCRIPTION_SHORT":    q.DESCRIPTION_SHORT = tb.Text; break;
            case "DESCRIPTION":          q.DESCRIPTION = tb.Text; break;
            case "QUEST_REQUIRED":       q.QUEST_REQUIRED = tb.Text; break;
            case "QUEST_UNAVAILABLE":    q.QUEST_UNAVAILABLE = tb.Text; break;
            case "GLOBAL_TYPE":          if (int.TryParse(tb.Text, out var gt))  q.GLOBAL_TYPE = gt; break;
            case "RELOAD_QUEST":         if (int.TryParse(tb.Text, out var rq))  q.RELOAD_QUEST = rq; break;
            case "TIME_LIMIT_FOR_QUEST": if (int.TryParse(tb.Text, out var tl))  q.TIME_LIMIT_FOR_QUEST = tl; break;
            case "CAN_CANCEL_QUEST":     if (int.TryParse(tb.Text, out var cc))  q.CAN_CANCEL_QUEST = cc; break;
            case "REPUTATION_REWARD":    if (double.TryParse(tb.Text, out var rr)) q.REPUTATION_REWARD = rr; break;
            case "REPUTATION_PENALTY":   if (double.TryParse(tb.Text, out var rp)) q.REPUTATION_PENALTY = rp; break;
            case "NEED_REPUTATION":      if (double.TryParse(tb.Text, out var nr)) q.NEED_REPUTATION = nr; break;
            case "UNIQUE":               if (int.TryParse(tb.Text, out var un))  q.UNIQUE = un; break;
            case "HOUR_START":           if (int.TryParse(tb.Text, out var hs))  q.HOUR_START = hs; break;
            case "MINUTE_START":         if (int.TryParse(tb.Text, out var ms))  q.MINUTE_START = ms; break;
            case "HOUR_FINISH":          if (int.TryParse(tb.Text, out var hf))  q.HOUR_FINISH = hf; break;
            case "MINUTE_FINISH":        if (int.TryParse(tb.Text, out var mf))  q.MINUTE_FINISH = mf; break;
        }
    }

    private void DayQuest_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (QuestsList.SelectedItem is not QuestFileItem q) return;

        q.DAY_QUEST = DayQuestPanel.Children
            .OfType<CheckBox>()
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag?.ToString() ?? "")
            .Where(s => s.Length > 0)
            .ToList();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Квесты — TARGETS
    // ═════════════════════════════════════════════════════════════════════════

    private void AddTarget_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;

        var target = new QuestTarget { COUNT = 1, NEED_LIQUID = -1 };
        if (!OpenTargetDialog(target)) return;

        q.TARGETS.Add(target);
        RefreshQuestSubGrids(q);
    }

    private void DeleteTarget_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        if (TargetsGrid.SelectedItem is not QuestTarget t) return;
        q.TARGETS.Remove(t);
        RefreshQuestSubGrids(q);
    }

    private void EditTarget_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        if (TargetsGrid.SelectedItem is not QuestTarget t) return;

        // Редактируем копию — применяем только при OK
        var copy = CloneTarget(t);
        if (!OpenTargetDialog(copy)) return;

        var idx = q.TARGETS.IndexOf(t);
        q.TARGETS[idx] = copy;
        RefreshQuestSubGrids(q);
    }

    private void TargetsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        EditTarget_Click(sender, e);
    }

    private bool OpenTargetDialog(QuestTarget target)
    {
        var dlg = new QuestTargetEditDialog(target, _itemDatabase) { Owner = this };
        return dlg.ShowDialog() == true;
    }

    private static QuestTarget CloneTarget(QuestTarget t)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(t);
        return System.Text.Json.JsonSerializer.Deserialize<QuestTarget>(json)!;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Квесты — REWARDS
    // ═════════════════════════════════════════════════════════════════════════

    // ── Reward/Cost autocomplete ──────────────────────────────────────────────
    private void RewardItemCombo_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is ComboBox cb) FilterComboBox(cb, cb.Text?.Trim() ?? "");
    }

    private void CostItemCombo_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is ComboBox cb) FilterComboBox(cb, cb.Text?.Trim() ?? "");
    }

        private void AddReward_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        q.REWARDS.Add(new QuestReward { COUNT = 1, SET_LIQUID = -1, SETTINGS = "-1" });
        RefreshQuestSubGrids(q);
    }

    private void DeleteReward_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        if (RewardsGrid.SelectedItem is not QuestReward r) return;
        q.REWARDS.Remove(r);
        RefreshQuestSubGrids(q);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Квесты — COST_QUEST
    // ═════════════════════════════════════════════════════════════════════════

    private void AddCost_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        q.COST_QUEST.Add(new QuestCostItem { COUNT = 1, NEED_LIQUID = -1 });
        RefreshQuestSubGrids(q);
    }

    private void DeleteCost_Click(object sender, RoutedEventArgs e)
    {
        if (QuestsList.SelectedItem is not QuestFileItem q) return;
        if (CostGrid.SelectedItem is not QuestCostItem c) return;
        q.COST_QUEST.Remove(c);
        RefreshQuestSubGrids(q);
    }

    private void RefreshQuestSubGrids(QuestFileItem? q)
    {
        TargetsGrid.ItemsSource = null; TargetsGrid.ItemsSource = q?.TARGETS;
        RewardsGrid.ItemsSource = null; RewardsGrid.ItemsSource = q?.REWARDS;
        CostGrid.ItemsSource    = null; CostGrid.ItemsSource    = q?.COST_QUEST;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // NPC
    // ═════════════════════════════════════════════════════════════════════════

    private void NpcList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var npc = NpcList.SelectedItem as QuestNpc;
        NpcDetailPanel.IsEnabled = npc != null;
        LoadNpcFields(npc);
    }

    private void LoadNpcFields(QuestNpc? n)
    {
        _npcLoading = true;
        try
        {
            NN_Id.Text         = n?.NPC_ID.ToString() ?? "";
            NN_Type.Text       = n?.NPC_TYPE ?? "";
            NN_Name.Text       = n?.NPC_NAME ?? "";
            NN_NameAction.Text = n?.NPC_NAME_FOR_ACTION ?? "";
            NN_Role.Text       = n?.NPC_ROLE ?? "";
            NN_Transfer.Text   = n?.ENABLE_TRANSFER.ToString() ?? "";
            NN_Pos.Text        = n?.POSITION ?? "";
            NN_Orient.Text     = n?.ORIENTATION ?? "";
            NN_Avatar.Text     = n?.NPC_AVATAR_PATH ?? "";
            NN_Desc.Text       = n?.NPC_DESCRIPTION ?? "";
            NN_QStart.Text     = n?.QUEST_START ?? "";
            NN_QFinish.Text    = n?.QUEST_FINISH ?? "";
            NN_RepLow.Text     = n?.REPUTATION_SETTINGS?.LOW_REPUTATION.ToString() ?? "";
            NN_RepHigh.Text    = n?.REPUTATION_SETTINGS?.HIGH_REPUTATION.ToString() ?? "";

            NpcAttachList.ItemsSource  = n?.ATTACHMENTS;
            NpcDialogsGrid.ItemsSource = n?.DIALOG_SETTINGS;
        }
        finally { _npcLoading = false; }
    }

    private void NpcField_Changed(object sender, TextChangedEventArgs e)
    {
        if (_npcLoading) return;
        _autoSave.MarkDirty("questcfg");
        if (NpcList.SelectedItem is not QuestNpc n) return;
        if (sender is not TextBox tb) return;

        switch (tb.Tag?.ToString())
        {
            case "NPC_TYPE":            n.NPC_TYPE = tb.Text; break;
            case "NPC_NAME":            n.NPC_NAME = tb.Text; NpcList.Items.Refresh(); break;
            case "NPC_NAME_FOR_ACTION": n.NPC_NAME_FOR_ACTION = tb.Text; break;
            case "NPC_ROLE":            n.NPC_ROLE = tb.Text; break;
            case "NPC_DESCRIPTION":     n.NPC_DESCRIPTION = tb.Text; break;
            case "POSITION":            n.POSITION = tb.Text; break;
            case "ORIENTATION":         n.ORIENTATION = tb.Text; break;
            case "NPC_AVATAR_PATH":     n.NPC_AVATAR_PATH = tb.Text; break;
            case "QUEST_START":         n.QUEST_START = tb.Text; break;
            case "QUEST_FINISH":        n.QUEST_FINISH = tb.Text; break;
            case "ENABLE_TRANSFER":     if (int.TryParse(tb.Text, out var t)) n.ENABLE_TRANSFER = t; break;
            case "REP_LOW":             if (double.TryParse(tb.Text, out var rl)) n.REPUTATION_SETTINGS.LOW_REPUTATION = rl; break;
            case "REP_HIGH":            if (double.TryParse(tb.Text, out var rh)) n.REPUTATION_SETTINGS.HIGH_REPUTATION = rh; break;
        }
    }

    private void AddNpc_Click(object sender, RoutedEventArgs e)
    {
        if (_questConfig == null) { MessageBox.Show("Сначала открой GM_QuestSystemCFG."); return; }

        var npc = new QuestNpc
        {
            NPC_ID      = GetNextNpcId(),
            NPC_NAME    = "Новый NPC",
            NPC_TYPE    = "GM_NPC_",
            POSITION    = "0 0 0",
            ORIENTATION = "0 0 0",
            REPUTATION_SETTINGS = new NpcReputationSettings { HIGH_REPUTATION = 5000 }
        };

        _questConfig.NPC.Add(npc);
        NpcList.ItemsSource = null;
        NpcList.ItemsSource = _questConfig.NPC;
        NpcList.SelectedItem = npc;
        NpcList.ScrollIntoView(npc);
        SetStatus($"NPC ID={npc.NPC_ID} добавлен");
    }

    private void DeleteNpc_Click(object sender, RoutedEventArgs e)
    {
        if (_questConfig == null || NpcList.SelectedItem is not QuestNpc npc) return;

        if (MessageBox.Show($"Удалить NPC «{npc.NPC_NAME}»?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        var json = System.Text.Json.JsonSerializer.Serialize(_questConfig.NPC);
        var name = npc.NPC_NAME;
        _snapshots.Push($"Удаление NPC «{name}»", json, j => {
            var restored = System.Text.Json.JsonSerializer.Deserialize<List<QuestNpc>>(j)!;
            _questConfig!.NPC.Clear();
            foreach (var n in restored) _questConfig.NPC.Add(n);
            NpcList.ItemsSource = null;
            NpcList.ItemsSource = _questConfig.NPC;
        });

        _questConfig.NPC.Remove(npc);
        NpcList.ItemsSource = null;
        NpcList.ItemsSource = _questConfig.NPC;
        _autoSave.MarkDirty("questcfg");
        SetStatus($"NPC «{name}» удалён");
    }

    private void DuplicateNpc_Click(object sender, RoutedEventArgs e)
    {
        if (_questConfig == null || NpcList.SelectedItem is not QuestNpc original) return;
        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var copy = System.Text.Json.JsonSerializer.Deserialize<QuestNpc>(json)!;
        copy.NPC_ID = GetNextNpcId();
        copy.NPC_NAME = original.NPC_NAME + " (копия)";
        _questConfig.NPC.Add(copy);
        NpcList.ItemsSource = null;
        NpcList.ItemsSource = _questConfig.NPC;
        NpcList.SelectedItem = copy;
        SetStatus($"NPC скопирован → ID={copy.NPC_ID}");
    }

    // Экипировка NPC
    private void NpcAttachCombo_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is ComboBox cb) FilterComboBox(cb, cb.Text?.Trim() ?? "");
    }

    private void AddNpcAttach_Click(object sender, RoutedEventArgs e)
    {
        if (NpcList.SelectedItem is not QuestNpc npc) return;
        var val = NpcAttachCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(val)) return;
        npc.ATTACHMENTS.Add(val);
        NpcAttachCombo.Text = "";
        NpcAttachList.ItemsSource = null;
        NpcAttachList.ItemsSource = npc.ATTACHMENTS;
    }

    private void DeleteNpcAttach_Click(object sender, RoutedEventArgs e)
    {
        if (NpcList.SelectedItem is not QuestNpc npc) return;
        if (NpcAttachList.SelectedItem is not string item) return;
        npc.ATTACHMENTS.Remove(item);
        NpcAttachList.ItemsSource = null;
        NpcAttachList.ItemsSource = npc.ATTACHMENTS;
    }

    // Диалоги NPC
    private void AddNpcDialog_Click(object sender, RoutedEventArgs e)
    {
        if (NpcList.SelectedItem is not QuestNpc npc) return;
        npc.DIALOG_SETTINGS.Add(new NpcDialog { ID = $"dialog_{npc.DIALOG_SETTINGS.Count + 1}" });
        NpcDialogsGrid.ItemsSource = null;
        NpcDialogsGrid.ItemsSource = npc.DIALOG_SETTINGS;
    }

    private void DeleteNpcDialog_Click(object sender, RoutedEventArgs e)
    {
        if (NpcList.SelectedItem is not QuestNpc npc) return;
        if (NpcDialogsGrid.SelectedItem is not NpcDialog dlg) return;
        npc.DIALOG_SETTINGS.Remove(dlg);
        NpcDialogsGrid.ItemsSource = null;
        NpcDialogsGrid.ItemsSource = npc.DIALOG_SETTINGS;
    }

    // NpcGrid_SelectionChanged оставляем пустым для обратной совместимости
    private void NpcGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    // ═════════════════════════════════════════════════════════════════════════
    // Пресеты лута
    // ═════════════════════════════════════════════════════════════════════════

    private void LootPresetsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LootPresetsList.SelectedItem is not LootPreset preset)
        {
            StashPosList.ItemsSource  = null;
            LootItemsGrid.ItemsSource = null;
            return;
        }

        StashPosList.ItemsSource  = preset.STASH_POSITIONS;
        LootItemsGrid.ItemsSource = preset.ITEMS_LIST;
    }

    private void AddLootPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null) { MessageBox.Show("Сначала открой GM_PRESET_CONFIG."); return; }

        var preset = new LootPreset
        {
            LOOT_PRESET = _presetConfig.PRESET_SETTINGS.Count == 0
                ? 1 : _presetConfig.PRESET_SETTINGS.Max(p => p.LOOT_PRESET) + 1
        };

        _presetConfig.PRESET_SETTINGS.Add(preset);
        RefreshLootPresetsList();
        LootPresetsList.SelectedItem = preset;
    }

    private void DeleteLootPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null || LootPresetsList.SelectedItem is not LootPreset preset) return;
        _presetConfig.PRESET_SETTINGS.Remove(preset);
        RefreshLootPresetsList();
    }

    private void AddStashPos_Click(object sender, RoutedEventArgs e)
    {
        if (LootPresetsList.SelectedItem is not LootPreset preset) return;
        var x = StashPosX.Text.Trim();
        var y = StashPosY.Text.Trim();
        var z = StashPosZ.Text.Trim();
        if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y) || string.IsNullOrWhiteSpace(z))
        {
            SetStatus("Заполни все три поля X, Y, Z");
            return;
        }
        preset.STASH_POSITIONS.Add($"{x} {y} {z}");
        StashPosX.Clear(); StashPosY.Clear(); StashPosZ.Clear();
        StashPosList.ItemsSource = null;
        StashPosList.ItemsSource = preset.STASH_POSITIONS;
    }

    private void DeleteStashPos_Click(object sender, RoutedEventArgs e)
    {
        if (LootPresetsList.SelectedItem is not LootPreset preset) return;
        if (StashPosList.SelectedItem is not string pos) return;
        preset.STASH_POSITIONS.Remove(pos);
        StashPosList.ItemsSource = null;
        StashPosList.ItemsSource = preset.STASH_POSITIONS;
    }

    private void LootItemCombo_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is ComboBox cb) FilterComboBox(cb, cb.Text?.Trim() ?? "");
    }

    private void AddLootItem_Click(object sender, RoutedEventArgs e)
    {
        if (LootPresetsList.SelectedItem is not LootPreset preset) return;
        var className = LootItemCombo.Text?.Trim() ?? "";
        preset.ITEMS_LIST.Add(new LootPresetItem { CLASSNAME = className, CHANCE_TO_CREATE = 1.0, COUNT_ITEM = 1, HEALTH_ITEM = 1.0 });
        LootItemCombo.Text = "";
        LootItemsGrid.ItemsSource = null;
        LootItemsGrid.ItemsSource = preset.ITEMS_LIST;
    }

    private void DeleteLootItem_Click(object sender, RoutedEventArgs e)
    {
        if (LootPresetsList.SelectedItem is not LootPreset preset) return;
        if (LootItemsGrid.SelectedItem is not LootPresetItem item) return;
        preset.ITEMS_LIST.Remove(item);
        LootItemsGrid.ItemsSource = null;
        LootItemsGrid.ItemsSource = preset.ITEMS_LIST;
    }

    private void RefreshLootPresetsList()
    {
        if (_presetConfig == null) return;
        LootPresetsList.ItemsSource = null;
        LootPresetsList.ItemsSource = _presetConfig.PRESET_SETTINGS;
        LootPresetsList.DisplayMemberPath = "LOOT_PRESET";
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Триггеры
    // ═════════════════════════════════════════════════════════════════════════

    private void AddTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null) { MessageBox.Show("Сначала открой GM_PRESET_CONFIG."); return; }

        var trigger = new TriggerPreset
        {
            TRIGGER_ID     = _presetConfig.TRIGGER_SETTINGS.Count == 0
                ? 1 : _presetConfig.TRIGGER_SETTINGS.Max(t => t.TRIGGER_ID) + 1,
            POSITION       = "0 0 0",
            RADIUS         = 15.0,
            AI_SPAWN_RADIUS = 8.0,
            AI_SETTINGS    = new AiSettings { COUNT_AI = 1 }
        };

        _presetConfig.TRIGGER_SETTINGS.Add(trigger);
        TriggersGrid.Items.Refresh();
    }

    private void DeleteTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null || TriggersGrid.SelectedItem is not TriggerPreset t) return;
        _presetConfig.TRIGGER_SETTINGS.Remove(t);
        TriggersGrid.Items.Refresh();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Маппинг
    // ═════════════════════════════════════════════════════════════════════════

    private void AddMappingGroup_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null) { MessageBox.Show("Сначала открой GM_PRESET_CONFIG."); return; }

        var group = new MappingGroup
        {
            GROUP_ID   = _presetConfig.MAPPING_SETTINGS.Count == 0
                ? 1 : _presetConfig.MAPPING_SETTINGS.Max(g => g.GROUP_ID) + 1,
            GROUP_NAME = "Новая группа"
        };

        _presetConfig.MAPPING_SETTINGS.Add(group);
        MappingGrid.Items.Refresh();
    }

    private void DeleteMappingGroup_Click(object sender, RoutedEventArgs e)
    {
        if (_presetConfig == null || MappingGrid.SelectedItem is not MappingGroup g) return;
        _presetConfig.MAPPING_SETTINGS.Remove(g);
        MappingGrid.Items.Refresh();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private int GetNextQuestId() =>
        _quests.Count == 0 ? 1 : _quests.Max(q => q.ID) + 1;

    private int GetNextNpcId() =>
        _questConfig == null || _questConfig.NPC.Count == 0
            ? 1 : _questConfig.NPC.Max(n => n.NPC_ID) + 1;

    private static string? EnsureSavePath(ref string? path, string defaultName)
    {
        if (!string.IsNullOrWhiteSpace(path)) return path;

        var dlg = new SaveFileDialog
        {
            Filter   = "JSON files (*.json)|*.json",
            FileName = defaultName
        };

        if (dlg.ShowDialog() != true) return null;
        path = dlg.FileName;
        return path;
    }

    private void NormalizeQuests()
    {
        foreach (var q in _quests)
        {
            q.TARGETS           ??= new List<QuestTarget>();
            q.REWARDS           ??= new List<QuestReward>();
            q.COST_QUEST        ??= new List<QuestCostItem>();   // ← исправлено
            q.DAY_QUEST         ??= new List<string>();
            q.IS_SPECIAL_SETTINGS ??= new List<SpecialQuestSettings>();
        }
    }

    private void ShowError(string msg, Exception ex) =>
        MessageBox.Show($"{msg}\n\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
}

// ─── Вспомогательный класс для отображения прогресса ────────────────────────

public class QuestInProgressDisplay
{
    public int    ID              { get; }
    public long   SYSTEM_ID      { get; }
    public int    IS_FINISH      { get; }
    public string ProgressDisplay { get; }

    public QuestInProgressDisplay(QuestInProgress q)
    {
        ID              = q.ID;
        SYSTEM_ID       = q.SYSTEM_ID;
        IS_FINISH       = q.IS_FINISH;
        ProgressDisplay = q.PROGRESS_TARGETS.Count == 0
            ? "—"
            : string.Join(" / ", q.PROGRESS_TARGETS.Select(v => v.ToString("F0")));
    }
}