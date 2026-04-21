using Microsoft.AspNetCore.Components.Web;
using VoucherGenerator.Domain.Entities;

namespace VoucherGenerator.Web.Components.Pages;

public partial class History
{
    private List<Voucher> Vouchers = new();
    private string SearchQuery = string.Empty;
    private bool IsLoading = true;
    private int CurrentPage = 1;
    private const int PageSize = 100;

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(Vouchers.Count / (double)PageSize));
    private IEnumerable<Voucher> PagedVouchers => Vouchers.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
    private int PageStart => Vouchers.Count == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    private int PageEnd => Math.Min(CurrentPage * PageSize, Vouchers.Count);
    private IEnumerable<int> VisiblePages
    {
        get
        {
            var start = Math.Max(1, CurrentPage - 1);
            var end = Math.Min(TotalPages, start + 2);

            if (end - start < 2)
            {
                start = Math.Max(1, end - 2);
            }

            return Enumerable.Range(start, end - start + 1);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAll();
    }

    private async Task LoadAll()
    {
        IsLoading = true;
        Vouchers = await VoucherService.GetHistoryAsync();
        CurrentPage = 1;
        IsLoading = false;
    }

    private async Task Search()
    {
        IsLoading = true;
        if (string.IsNullOrWhiteSpace(SearchQuery))
            Vouchers = await VoucherService.GetHistoryAsync();
        else
            Vouchers = await VoucherService.SearchHistoryAsync(SearchQuery.Trim());
        CurrentPage = 1;
        IsLoading = false;
    }

    private async Task ClearSearch()
    {
        SearchQuery = string.Empty;
        await LoadAll();
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await Search();
    }

    private void GoToPage(int page)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
    }

    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }
}
