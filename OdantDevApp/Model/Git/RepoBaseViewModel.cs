using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SharedOdantDev.Model;

public class RepoLoadingViewModel : RepoBaseViewModel
{
    public RepoLoadingViewModel() : base(null, null, null) { }
    public override string Name => "Loading";
}

public partial class RepoBaseViewModel : ObservableObject
{
    static readonly IEnumerable<RepoBaseViewModel> dummyList = new[] { new RepoLoadingViewModel() };

    public RepoBaseViewModel(BaseGitItem item, BaseGitItem parent, OdantDev.Model.ILogger logger = null)
    {
        Item = item;
        Parent = parent;
        this.logger = logger;
        children = CanBeExpanded ? dummyList : null;
    }

    protected virtual bool CanBeExpanded => true;

    [ObservableProperty]
    protected BaseGitItem item;

    [ObservableProperty]
    protected BaseGitItem parent;

    protected OdantDev.Model.ILogger logger { get; }

    public virtual string Name => string.Empty;

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
        var task = Task.Run(() => GetChildrenAsync());

        if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))))
        {
            var children = await task;
            Children = children;
        }
        else
        {
            logger?.Info($"Timeout when getting children for {this}");
            Children = null;
        }
    }
    public virtual Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync() => Task.FromResult(Enumerable.Empty<RepoBaseViewModel>());

    public override string ToString() => Name;
}
