using App.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Components
{
    [ViewComponent]
    public class CategorySideBar : ViewComponent
    {
        public class CategorySideBarData
        {
            public List<Category> Categories { set; get; }
            public int level { set; get; }
            public string slugCategory { set; get; }
            public List<Brand> brands { set; get; }
            public string slugBrand { set; get; }
        }

        public const string COMPONENTNAME = "CategorySideBar";
        public CategorySideBar() { }
        public IViewComponentResult Invoke(CategorySideBarData data)
        {
            return View(data);
        }
    }
}