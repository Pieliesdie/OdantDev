using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

using GitLabApiClient.Models.Groups.Responses;

namespace SharedOdantDev.Model
{
    public abstract class BaseGitItem
    {
        protected static Dictionary<string, ImageSource> _images = new();
        public static Pen Pen { get; set; } = new(Brushes.Black, 0.1);

        public static Brush Brush { get; set; } = Brushes.Black;

        public abstract string Name { get; }

        public abstract object Object { get; }

        public virtual string FullPath { get; protected set; }

        protected abstract string ImageCode { get; }
        public ImageSource Icon
        {
            get
            {
                if (_images.TryGetValue(ImageCode, out ImageSource imageSource))
                {
                    return imageSource;
                }

                return GetImageSource(ImageCode, Pen, Brush, null, null, null);
            }
        }

        public virtual bool HasModule { get; set; }

        private static ImageSource GetImageSource(string geometry, Pen pen, Brush brush, Transform transform, IValueConverter converter, object parameter)
        {
            var geom = new GeometryDrawing
            {
                Geometry = Geometry.Parse(geometry),
            };
            if (pen != null)
            {
                // geom.Pen = pen;
            }
            if (brush != null)
            {
                geom.Brush = brush;
            }
            var grp = new DrawingGroup
            {
                Transform = transform,
                Children = { geom },
            };
            ImageSource result = new DrawingImage
            {
                Drawing = grp,
            };
            if (converter != null)
            {
                result = (ImageSource)converter.Convert(result, typeof(ImageSource), parameter, CultureInfo.CurrentCulture);
            }
            return result;
        }
    }

    public class GroupItem : BaseGitItem
    {
        private readonly Group _group;


        public GroupItem(Group group)
        {
            _group = group;
            FullPath = _group.FullPath;
        }

        public override string Name => _group.Name;

        public override object Object => _group;

        public override bool HasModule => false;

        protected override string ImageCode => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z";
    }

    public class RootItem : BaseGitItem
    {
        public RootItem(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        public override object Object { get; }

        public override bool HasModule => false;

        protected override string ImageCode => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z";
    }

    public class ProjectItem : BaseGitItem
    {
        private readonly GitLabApiClient.Models.Projects.Responses.Project _project;

        public ProjectItem(GitLabApiClient.Models.Projects.Responses.Project project)
        {
            _project = project;
        }

        public override string Name => $"{_project.Name} ({_project.Path})";

        public override object Object => _project;

        public override bool HasModule => true;
        private async Task<bool> IsHasModule()
        {
            return await GitClient.FindTopOclFileAsync(_project) != null;
        }

        protected override string ImageCode => "M17,18L12,15.82L7,18V5H17M17,3H7A2,2 0 0,0 5,5V21L12,18L19,21V5C19,3.89 18.1,3 17,3Z";
    }
}
