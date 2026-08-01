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
    public class FlightChangeHistoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightChangeHistoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FlightChangeHistories
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.FlightChangeHistories.Include(f => f.FlightChangeRequest);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: FlightChangeHistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeHistory = await _context.FlightChangeHistories
                .Include(f => f.FlightChangeRequest)
                .FirstOrDefaultAsync(m => m.FlightChangeHistoryId == id);
            if (flightChangeHistory == null)
            {
                return NotFound();
            }

            return View(flightChangeHistory);
        }

        // GET: FlightChangeHistories/Create
        public IActionResult Create()
        {
            ViewData["FlightChangeRequestId"] = new SelectList(_context.FlightChangeRequests, "FlightChangeRequestId", "Status");
            return View();
        }

        // POST: FlightChangeHistories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlightChangeHistoryId,FlightChangeRequestId,PreviousStatus,NewStatus,ChangeDate,ChangedBy,Observation")] FlightChangeHistory flightChangeHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flightChangeHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["FlightChangeRequestId"] = new SelectList(_context.FlightChangeRequests, "FlightChangeRequestId", "Status", flightChangeHistory.FlightChangeRequestId);
            return View(flightChangeHistory);
        }

        // GET: FlightChangeHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeHistory = await _context.FlightChangeHistories.FindAsync(id);
            if (flightChangeHistory == null)
            {
                return NotFound();
            }
            ViewData["FlightChangeRequestId"] = new SelectList(_context.FlightChangeRequests, "FlightChangeRequestId", "Status", flightChangeHistory.FlightChangeRequestId);
            return View(flightChangeHistory);
        }

        // POST: FlightChangeHistories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FlightChangeHistoryId,FlightChangeRequestId,PreviousStatus,NewStatus,ChangeDate,ChangedBy,Observation")] FlightChangeHistory flightChangeHistory)
        {
            if (id != flightChangeHistory.FlightChangeHistoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flightChangeHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlightChangeHistoryExists(flightChangeHistory.FlightChangeHistoryId))
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
            ViewData["FlightChangeRequestId"] = new SelectList(_context.FlightChangeRequests, "FlightChangeRequestId", "Status", flightChangeHistory.FlightChangeRequestId);
            return View(flightChangeHistory);
        }

        // GET: FlightChangeHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeHistory = await _context.FlightChangeHistories
                .Include(f => f.FlightChangeRequest)
                .FirstOrDefaultAsync(m => m.FlightChangeHistoryId == id);
            if (flightChangeHistory == null)
            {
                return NotFound();
            }

            return View(flightChangeHistory);
        }

        // POST: FlightChangeHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flightChangeHistory = await _context.FlightChangeHistories.FindAsync(id);
            if (flightChangeHistory != null)
            {
                _context.FlightChangeHistories.Remove(flightChangeHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlightChangeHistoryExists(int id)
        {
            return _context.FlightChangeHistories.Any(e => e.FlightChangeHistoryId == id);
        }
    }
}
