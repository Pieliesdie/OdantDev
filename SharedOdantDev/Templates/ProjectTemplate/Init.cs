using oda;
using oda.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oda
{
    public sealed class Init : MainInit
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

        [Modifieres(MethodArea.Protected)]
        [DisplayName("SampleButton")]
        [RunContext(ItemType.Object)]
        [Active(true)]
        [ViewMode(ViewModes.ServiceButton)]
        [ViewContext(ItemType.Object | ItemType.Class)]
        [Icon(Icons.Run)]
        public void SampleButton()
        {
            return; // your code here
        }
    }
}
