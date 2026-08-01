using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Models.Application;

namespace U3_Examen_Airport.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministrationController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdministrationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchText,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortOrder = "date_desc",
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is 5 or 10 or 20 ? pageSize : 10;
        searchText = string.IsNullOrWhiteSpace(searchText)
            ? null
            : searchText.Trim();

        var allowedStatuses = new[]
        {
            "Pendiente",
            "Aprobado",
            "Cancelado",
            "Rechazado",
            "Fallido"
        };

        status = allowedStatuses.Contains(status) ? status : null;
        sortOrder = sortOrder is "date_asc" or "date_desc"
            or "amount_asc" or "amount_desc"
            ? sortOrder
            : "date_desc";

        var totalOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var approvedPaymentsQuery = _context.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == "Aprobado");

        var approvedPayments = await approvedPaymentsQuery
            .CountAsync(cancellationToken);
        var approvedRevenue = await approvedPaymentsQuery
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        var averageApprovedPayment = await approvedPaymentsQuery
            .AverageAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        var ordersByStatus = await _context.Orders
            .AsNoTracking()
            .GroupBy(order => order.Status)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Count(),
                cancellationToken);

        var ordersQuery = _context.Orders
            .AsNoTracking()
            .AsQueryable();

        if (searchText is not null)
        {
            if (int.TryParse(searchText, out var orderId))
            {
                ordersQuery = ordersQuery.Where(order =>
                    order.OrderId == orderId
                    || _context.Users.Any(user =>
                        user.Id == order.UserId
                        && user.Email != null
                        && EF.Functions.ILike(user.Email, $"%{searchText}%")));
            }
            else
            {
                ordersQuery = ordersQuery.Where(order =>
                    _context.Users.Any(user =>
                        user.Id == order.UserId
                        && user.Email != null
                        && EF.Functions.ILike(user.Email, $"%{searchText}%")));
            }
        }

        if (status is not null)
        {
            ordersQuery = ordersQuery.Where(order => order.Status == status);
        }

        if (dateFrom.HasValue)
        {
            ordersQuery = ordersQuery.Where(
                order => order.CreationDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            var exclusiveDateTo = dateTo.Value.Date.AddDays(1);
            ordersQuery = ordersQuery.Where(
                order => order.CreationDate < exclusiveDateTo);
        }

        var totalRecords = await ordersQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Min(page, totalPages);

        ordersQuery = sortOrder switch
        {
            "date_asc" => ordersQuery.OrderBy(order => order.CreationDate),
            "amount_asc" => ordersQuery.OrderBy(order => order.TotalAmount),
            "amount_desc" => ordersQuery.OrderByDescending(order => order.TotalAmount),
            _ => ordersQuery.OrderByDescending(order => order.CreationDate)
        };

        var orders = await ordersQuery
            .Include(order => order.FlightChangeRequest)
            .Include(order => order.Payments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = orders
            .Select(order => order.UserId)
            .Distinct()
            .ToArray();

        var userEmails = await _context.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new { user.Id, user.Email })
            .ToDictionaryAsync(
                user => user.Id,
                user => user.Email ?? "Sin correo",
                cancellationToken);

        var recentTransactions = await _context.TransactionHistories
            .AsNoTracking()
            .Include(transaction => transaction.Payment)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        ViewBag.SearchText = searchText;
        ViewBag.Status = status;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.SortOrder = sortOrder;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.ApprovedPayments = approvedPayments;
        ViewBag.ApprovedRevenue = approvedRevenue;
        ViewBag.AverageApprovedPayment = averageApprovedPayment;
        ViewBag.OrdersByStatus = ordersByStatus;
        ViewBag.UserEmails = userEmails;
        ViewBag.RecentTransactions = recentTransactions;

        return View(orders);
    }
}
