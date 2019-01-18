using Sdl.MultiSelectComboBox.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AmiBroker.Controllers
{
    public class CustomFilterService : IFilterService
    {
        private readonly Regex _toMatchDash = new Regex(@"^(((([A-Z])|([a-z])){2})\-(([A-Z])|([a-z])){0,3})");
        private readonly Regex _toMatchSpace = new Regex(@"^(((([A-Z])|([a-z])){2})\s(([A-Z])|([a-z])){0,3})");

        private string _filterText;
        private string _auxiliaryText = string.Empty;

        public void SetFilter(string criteria)
        {
            _filterText = criteria;

            ConfigureFilter();
        }

        public Predicate<object> Filter { get; set; }

        private bool FilteringByName(object item)
        {
            bool found = false;
            if (!string.IsNullOrEmpty(_filterText))
            {
                if (item.GetType().IsSubclassOf(typeof(IController)))
                {
                    found = ((IController)item).ConnParam.AccName.ToLower().Contains(_filterText.ToLower()) ||
                            ((IController)item).Vendor.ToLower().Contains(_filterText.ToLower());
                }
                else if (item.GetType() == typeof(AccountInfo))
                {
                    found = ((AccountInfo)item).Controller.ConnParam.AccName.ToLower().Contains(_filterText.ToLower()) ||
                            ((AccountInfo)item).Controller.Vendor.ToLower().Contains(_filterText.ToLower()) ||
                            ((AccountInfo)item).Name.ToLower().Contains(_filterText.ToLower());
                }
            }
            
            return string.IsNullOrEmpty(_filterText) || found;
        }

        private void ConfigureFilter()
        {
                Filter = FilteringByName;
        }
    }
}
