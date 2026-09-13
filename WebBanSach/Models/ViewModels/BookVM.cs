using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebBanSach.Models.ViewModels
{
    public class BookVM
    {
        public Book Book { get; set; } = new Book();

        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
