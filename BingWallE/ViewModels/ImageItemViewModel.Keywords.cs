using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BingWallE.Services;

namespace BingWallE.ViewModels;

public sealed record KeywordChip(string Label);

public partial class ImageItemViewModel
{
    public ObservableCollection<KeywordChip> KeywordChips { get; } = new();
    public ObservableCollection<KeywordChip> SuggestedKeywordChips { get; } = new();
    private string[] _storedKeywords = [];
    private readonly HashSet<string> _storedModelLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _userKeywords = new();
    private readonly HashSet<string> _excludedKeywords = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasKeywordChanges;
    public bool HasKeywordChanges { get => _hasKeywordChanges; private set => SetProperty(ref _hasKeywordChanges, value); }
    public bool KeywordsLoaded { get; private set; }

    public void LoadKeywords(KeywordMetadataState state, bool force = false)
    {
        if ((KeywordsLoaded || HasKeywordChanges) && !force) return;
        _storedKeywords = state.Keywords;
        _userKeywords.Clear(); _userKeywords.AddRange(state.UserKeywords);
        _excludedKeywords.Clear(); _excludedKeywords.UnionWith(state.ExcludedKeywords);
        _storedModelLabels.Clear();
        _storedModelLabels.UnionWith(WallpaperKeywords.Build(Image, state.Predictions).Where(x => x.FromModel).Select(x => x.Label));
        _predictions = state.Predictions;
        HasRecognition = state.Predictions.Length > 0;
        KeywordsLoaded = true; RebuildKeywords(); HasKeywordChanges = false;
    }

    public bool AddKeyword(string? value)
    {
        var keyword = value?.Trim();
        if (string.IsNullOrWhiteSpace(keyword) || keyword.Length > 100 || keyword.Contains(';') || keyword.Any(char.IsControl)) return false;
        if (_userKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase)) return false;
        _excludedKeywords.Remove(keyword);
        _userKeywords.Add(keyword); RebuildKeywords(); HasKeywordChanges = true;
        return true;
    }

    public void RemoveKeyword(string label)
    {
        _excludedKeywords.Add(label);
        _userKeywords.RemoveAll(x => x.Equals(label, StringComparison.OrdinalIgnoreCase));
        RebuildKeywords(); HasKeywordChanges = true;
    }

    public KeywordMetadataState ExportKeywords() => new(
        KeywordChips.Concat(SuggestedKeywordChips).Select(x => x.Label).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
        _excludedKeywords.ToArray(), _predictions.Where(p => !_excludedKeywords.Contains(p.Label)).ToArray(), _userKeywords.ToArray());

    private void RebuildKeywords()
    {
        var generated = WallpaperKeywords.Build(Image, _predictions);
        var modelLabels = generated.Where(x => x.FromModel).Select(x => x.Label).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        KeywordChips.Clear(); SuggestedKeywordChips.Clear();
        void Add(string label, bool model)
        {
            if (!_excludedKeywords.Contains(label) && values.Add(label))
                (model ? SuggestedKeywordChips : KeywordChips).Add(new(label));
        }
        foreach (var label in _userKeywords) Add(label, false);
        foreach (var label in _storedKeywords.Where(x => !modelLabels.Contains(x) && !_storedModelLabels.Contains(x))) Add(label, false);
        foreach (var keyword in generated.Where(x => !x.FromModel)) Add(keyword.Label, false);
        foreach (var keyword in generated.Where(x => x.FromModel)) Add(keyword.Label, true);
        OnPropertyChanged(nameof(TextKeywords)); OnPropertyChanged(nameof(ModelKeywords));
        OnPropertyChanged(nameof(HasTextKeywords)); OnPropertyChanged(nameof(HasModelKeywords));
    }
}
