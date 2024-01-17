using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Petsy.Data;
using Petsy.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Petsy.Controllers
{
    public class PetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public PetsController(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        // GET: Pets
        public async Task<IActionResult> Index()
        {
            List<Pet> pets;

            if (!_memoryCache.TryGetValue("pets", out pets))
            {
                var applicationDbContext = _context.Pets.Include(p => p.Person).Include(p => p.Vaccines);
                pets = await applicationDbContext.ToListAsync();

                MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions();
                cacheOptions.SetPriority(CacheItemPriority.Low);
                cacheOptions.SetSlidingExpiration(new TimeSpan(0, 0, 15));
                cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 0, 30));

                _memoryCache.Set("pets", pets, cacheOptions);
            }

            return View(pets);
        }
        // GET: Pets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Pets == null)
            {
                return NotFound();
            }

            var pet = await _context.Pets
                .Include(p => p.Person)
                .Include(x => x.Vaccines)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        // GET: Pets/Create
        public IActionResult Create()
        {
            ViewData["PersonId"] = new SelectList(_context.People, "Id", "GetFullName");
            ViewData["VaccineId"] = new MultiSelectList(_context.Vaccines, "Id", "Name");
            return View();
        }
        // POST: Pets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Age,PersonId,VaccinesParams")] Pet pet)
        {
            if (ModelState.IsValid)
            {
                if (pet.VaccinesParams != null && pet.VaccinesParams.Any())
                {
                    foreach (var vaccineId in pet.VaccinesParams)
                    {
                        var vaccine = await _context.Vaccines.FindAsync(vaccineId);
                        if (vaccine != null)
                        {
                            pet.Vaccines.Add(vaccine);
                        }
                    }
                }

                _context.Add(pet);
                await _context.SaveChangesAsync();

                // Remove the "pets" cache entry to force fetching the updated list from the database
                _memoryCache.Remove("pets");

                return RedirectToAction(nameof(Index));
            }

            ViewData["PersonId"] = new SelectList(_context.People, "Id", "GetFullName", pet.PersonId);

            return View(pet);
        }

        // GET: Pets/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Pets == null)
            {
                return NotFound();
            }

            var pet = await _context.Pets.Include(m => m.Vaccines)
                .Include(p => p.Vaccines)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pet == null)
            {
                return NotFound();
            }

            ViewData["PersonId"] = new SelectList(_context.People, "Id", "GetFullName", pet.PersonId);
            ViewData["VaccineId"] = new MultiSelectList(_context.Vaccines, "Id", "Name", pet.Vaccines.Select(x => x.Id));
            return View(pet);
        }

        // POST: Pets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Age,PersonId,VaccinesParams")] Pet pet)
        {
            if (id != pet.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Load existing pet with vaccines
                    var existingPet = await _context.Pets.Include(p => p.Vaccines).FirstOrDefaultAsync(p => p.Id == id);

                    // Update pet properties
                    existingPet.Name = pet.Name;
                    existingPet.Description = pet.Description;
                    existingPet.Age = pet.Age;
                    existingPet.PersonId = pet.PersonId;

                    // Update vaccines for the pet
                    if (pet.VaccinesParams != null)
                    {
                        existingPet.Vaccines.Clear();
                        foreach (var vaccineId in pet.VaccinesParams)
                        {
                            var vaccine = await _context.Vaccines.FindAsync(vaccineId);
                            if (vaccine != null)
                            {
                                existingPet.Vaccines.Add(vaccine);
                            }
                        }
                    }

                    _context.Update(existingPet);
                    await _context.SaveChangesAsync();
                    // Remove cache after update
                    _memoryCache.Remove("pets");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PetExists(pet.Id))
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

            ViewData["PersonId"] = new SelectList(_context.People, "Id", "GetFullName", pet.PersonId);
            ViewBag.VaccineList = new MultiSelectList(_context.Vaccines, "Id", "Name", pet.Vaccines.Select(v => v.Id));

            return View(pet);
        }

        // GET: Pets/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Pets == null)
            {
                return NotFound();
            }

            var pet = await _context.Pets
                .Include(p => p.Person)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        // POST: Pets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Pets == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Pets'  is null.");
            }
            var pet = await _context.Pets.FindAsync(id);
            if (pet != null)
            {
                _context.Pets.Remove(pet);
                // Remove cache after deletion
                _memoryCache.Remove("pets");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PetExists(int id)
        {
            return (_context.Pets?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
