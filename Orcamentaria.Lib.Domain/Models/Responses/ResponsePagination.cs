namespace Orcamentaria.Lib.Domain.Models.Responses
{
    public class ResponsePagination
    {
        public int CurrentPage { get; set; } = 1;
        public int ItemsPerpage { get; set; } = 10;
        public int TotalItems { get; set; }

        public ResponsePagination(int currentPage, int itemsPerpage, int totalItems)
        {
            CurrentPage = currentPage;
            ItemsPerpage = itemsPerpage;
            TotalItems = totalItems;
        }
    }
}
