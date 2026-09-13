namespace WebBanSach.Models.ViewModels
{
    public class ShoppingCartVM
    {
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; } = new List<ShoppingCart>();
        public OrderHeader OrderHeader { get; set; } = new OrderHeader();
        public decimal OrderTotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal FinalTotal { get; set; }
        public string? CouponCode { get; set; }
        public Coupon? AppliedCoupon { get; set; }
    }
}
