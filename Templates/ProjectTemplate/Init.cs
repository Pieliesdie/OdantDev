using oda;
using oda.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateProject
{
    public class Init : MainInit
    {
        [MethodType(MethodType.Event)]
        [DisplayName("SampleEvent")]
        [RunContext(ItemType.Object | ItemType.Field)]
        [ViewContext(ItemType.Object | ItemType.Class | ItemType.Field)]
        [Modifieres(MethodArea.Public)]
        [Active(false)]
        public bool SampleEvent()
        {
            return true; // your code here
        }

        [Browsable(true)]
        [Modifieres(MethodArea.Public)]
        [DisplayName("SampleButton")]
        [ShortName("SampleButton")]
        [Description("SampleButton")]
        [AccessLevel(AccessLevel.RWC)]
        [Icon(Icons.Save)]
        [Active(false)]
        [UseList(true)]
        [ViewMode(ViewModes.ToolButton)]
        [ExcludeViewContext(ItemType.Object)]
        [Category("Main")]
        [SortIndex(-500)]
        public void SampleButton()
        {
            return; // your code here
        }
    }
}
