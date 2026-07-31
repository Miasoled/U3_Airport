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
    [Authorize]
    public class FlightChangeRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightChangeRequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FlightChangeRequests
        public async Task<IActionResult> Index()
        {
            return View(await _context.FlightChangeRequests.ToListAsync());
        }

        // GET: FlightChangeRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeRequest = await _context.FlightChangeRequests
                .FirstOrDefaultAsync(m => m.FlightChangeRequestId == id);
            if (flightChangeRequest == null)
            {
                return NotFound();
            }

            return View(flightChangeRequest);
        }

        // GET: FlightChangeRequests/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FlightChangeRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlightChangeRequestId,UserId,BookingId,OriginalFlightId,NewFlightId,RequestDate,OriginalPrice,NewPrice,FareDifference,PenaltyAmount,TotalAmount,Status,Reason")] FlightChangeRequest flightChangeRequest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(flightChangeRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(flightChangeRequest);
        }

        // GET: FlightChangeRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeRequest = await _context.FlightChangeRequests.FindAsync(id);
            if (flightChangeRequest == null)
            {
                return NotFound();
            }
            return View(flightChangeRequest);
        }

        // POST: FlightChangeRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FlightChangeRequestId,UserId,BookingId,OriginalFlightId,NewFlightId,RequestDate,OriginalPrice,NewPrice,FareDifference,PenaltyAmount,TotalAmount,Status,Reason")] FlightChangeRequest flightChangeRequest)
        {
            if (id != flightChangeRequest.FlightChangeRequestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flightChangeRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlightChangeRequestExists(flightChangeRequest.FlightChangeRequestId))
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
            return View(flightChangeRequest);
        }

        // GET: FlightChangeRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flightChangeRequest = await _context.FlightChangeRequests
                .FirstOrDefaultAsync(m => m.FlightChangeRequestId == id);
            if (flightChangeRequest == null)
            {
                return NotFound();
            }

            return View(flightChangeRequest);
        }

        // POST: FlightChangeRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flightChangeRequest = await _context.FlightChangeRequests.FindAsync(id);
            if (flightChangeRequest != null)
            {
                _context.FlightChangeRequests.Remove(flightChangeRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlightChangeRequestExists(int id)
        {
            return _context.FlightChangeRequests.Any(e => e.FlightChangeRequestId == id);
        }
    }
}
