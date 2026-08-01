using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Models.Application;
using Microsoft.AspNetCore.Authorization;

namespace U3_Examen_Airport.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class TransactionHistoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionHistoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TransactionHistories
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.TransactionHistories.Include(t => t.Payment);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: TransactionHistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transactionHistory = await _context.TransactionHistories
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(m => m.TransactionHistoryId == id);
            if (transactionHistory == null)
            {
                return NotFound();
            }

            return View(transactionHistory);
        }

        // GET: TransactionHistories/Create
        public IActionResult Create()
        {
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Currency");
            return View();
        }

        // POST: TransactionHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TransactionHistoryId,PaymentId,ExternalTransactionId,TransactionDate,Status,Amount,Gateway,ResponseData")] TransactionHistory transactionHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transactionHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Currency", transactionHistory.PaymentId);
            return View(transactionHistory);
        }

        // GET: TransactionHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transactionHistory = await _context.TransactionHistories.FindAsync(id);
            if (transactionHistory == null)
            {
                return NotFound();
            }
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Currency", transactionHistory.PaymentId);
            return View(transactionHistory);
        }

        // POST: TransactionHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TransactionHistoryId,PaymentId,ExternalTransactionId,TransactionDate,Status,Amount,Gateway,ResponseData")] TransactionHistory transactionHistory)
        {
            if (id != transactionHistory.TransactionHistoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transactionHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionHistoryExists(transactionHistory.TransactionHistoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaymentId"] = new SelectList(_context.Payments, "PaymentId", "Currency", transactionHistory.PaymentId);
            return View(transactionHistory);
        }

        // GET: TransactionHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transactionHistory = await _context.TransactionHistories
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(m => m.TransactionHistoryId == id);
            if (transactionHistory == null)
            {
                return NotFound();
            }

            return View(transactionHistory);
        }

        // POST: TransactionHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transactionHistory = await _context.TransactionHistories.FindAsync(id);
            if (transactionHistory != null)
            {
                _context.TransactionHistories.Remove(transactionHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TransactionHistoryExists(int id)
        {
            return _context.TransactionHistories.Any(e => e.TransactionHistoryId == id);
        }
    }
}
