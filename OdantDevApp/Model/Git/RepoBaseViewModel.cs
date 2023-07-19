using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using OdantDev;

namespace SharedOdantDev.Model;

public partial class RepoBaseViewModel : ObservableObject
{
    private static readonly IEnumerable<RepoBaseViewModel> dummyList = new[] { new RepoLoadingViewModel() };
    private IEnumerable<RepoBaseViewModel>? DefaultChildren => CanBeExpanded ? dummyList : null;

    public RepoBaseViewModel(BaseGitItem item, BaseGitItem parent, OdantDev.Model.ILogger logger = null)
    {
        Item = item;
        Parent = parent;
        this.logger = logger;
        children = DefaultChildren;
    }

    protected virtual bool CanBeExpanded => true;

    [ObservableProperty]
    protected BaseGitItem item;

    [ObservableProperty]
    protected BaseGitItem parent;

    protected OdantDev.Model.ILogger logger { get; }

    public virtual string Name => Item?.Name ?? string.Empty;

    public bool IsItemAvailable => Item != null;

    public bool HasChildren => Children?.Any() ?? false;

    public virtual ImageSource Icon => Item?.Icon;

    public virtual bool HasModule => false;

    private bool isLoaded;

    [ObservableProperty]
    bool isExpanded;
    async partial void OnIsExpandedChanged(bool value)
    {
        if (value && !isLoaded)
        {
            isLoaded = true;
            if (Children is null || Children == dummyList)
            {
                await SetChildrenAsync();
            }
        }
    }

    [ObservableProperty]
    public IEnumerable<RepoBaseViewModel> children;

    private async Task SetChildrenAsync()
    {
        void СlearState()
        {
            Children = DefaultChildren;
            isLoaded = false;
            IsExpanded = false;
        }
        try
        {
            Children = await Task.Run(GetChildrenAsync).WithTimeout(TimeSpan.FromSeconds(15));
        }
        catch (TimeoutException)
        {
            logger?.Info($"Timeout when getting children for {this}");
            СlearState();
        }
        catch
        {
            СlearState();
            throw;
        }
    }
    public virtual Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync() => Task.FromResult(Enumerable.Empty<RepoBaseViewModel>());

    public override string ToString() => Name;
}
