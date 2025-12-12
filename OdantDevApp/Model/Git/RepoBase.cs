using CommunityToolkit.Mvvm.ComponentModel;
using OdantDevApp.Model.Git.GitItems;
using OdantDevApp.Model.ViewModels.Settings;

namespace OdantDevApp.Model.Git;

public partial class RepoBase : ObservableObject
{
    private static readonly IEnumerable<RepoBase> dummyList = new[] { new RepoLoading() };
    private IEnumerable<RepoBase>? DefaultChildren => CanBeExpanded ? dummyList : null;

    public RepoBase(BaseGitItem? item, BaseGitItem? parent, OdantDev.Model.ILogger? logger = null)
    {
        Item = item;
        Parent = parent;
        this.Logger = logger;
        children = DefaultChildren;
    }

    protected virtual bool CanBeExpanded => true;

    [ObservableProperty] protected BaseGitItem? item;

    [ObservableProperty] protected BaseGitItem? parent;

    protected OdantDev.Model.ILogger? Logger { get; }

    public virtual string Name => Item?.Name ?? string.Empty;

    public bool IsItemAvailable => Item != null;

    public bool HasChildren => Children?.Any() ?? false;

    public virtual ImageSource? Icon => Item?.Icon;

    public virtual bool HasModule => false;

    private bool isLoaded;

    [ObservableProperty] private bool isExpanded;
    async partial void OnIsExpandedChanged(bool value)
    {
        if (!value || isLoaded) return;
        isLoaded = true;
        if (Children is null || Children == dummyList)
        {
            await SetChildrenAsync();
        }
    }

    [ObservableProperty]
    private IEnumerable<RepoBase>? children;

    private async Task SetChildrenAsync()
    {
        try
        {
            Children = await Task.Run(GetChildrenAsync).WithTimeout(TimeSpan.FromSeconds(AddinSettings.Instance.GitlabTimeout));
        }
        catch (TimeoutException)
        {
            Logger?.Info($"Timeout when getting children for {this}");
            СlearState();
        }
        catch
        {
            СlearState();
            throw;
        }

        return;

        void СlearState()
        {
            Children = DefaultChildren;
            isLoaded = false;
            IsExpanded = false;
        }
    }
    public virtual Task<IEnumerable<RepoBase>> GetChildrenAsync() => Task.FromResult(Enumerable.Empty<RepoBase>());

    public override string ToString() => Name;
}
