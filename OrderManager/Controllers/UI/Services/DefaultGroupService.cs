using Sdl.MultiSelectComboBox.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Controllers
{   
    public class ItemGroup : IItemGroup
    {
        public int Order { get; set; }
        public string Name { get; }

        public ItemGroup(int index, string name)
        {
            Order = index;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
    public class DefaultGroupService
    {
        private readonly static List<IItemGroup> _itemGroups;
        private readonly static List<string> _items = new List<string>();
        static DefaultGroupService()
        {
            _itemGroups = new List<IItemGroup> { new ItemGroup(-1, "Others") };
            _itemGroups.Add(new ItemGroup(0, "Interactive Broker"));
            _itemGroups.Add(new ItemGroup(1, "FuTu NiuNiu"));
            _items.Add("IB");
            _items.Add("FT");
        }

        public static IItemGroup GetItemGroup(string id)
        {
            int i = _items.IndexOf(id);
            return _itemGroups.FirstOrDefault(x => x.Order == i);
        }
    }
}
