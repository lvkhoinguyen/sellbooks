namespace WebBanSach.Models.ViewModels
{
    public class OrderVM
    {
        public OrderHeader OrderHeader { get; set; } = new();
        public IEnumerable<OrderDetail> OrderDetail { get; set; } = new List<OrderDetail>();
    }
}
