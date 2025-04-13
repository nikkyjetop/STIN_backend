using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrecanskaBackend.Models;
using StrecanskaBackend.Models.Tables;

namespace StrecanskaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteTickersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoriteTickersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/FavoriteTickers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FavoriteTicker>>> GetFavoriteTickers()
        {
            return await _context.FavoriteTickers.ToListAsync();
        }

        // GET: api/FavoriteTickers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FavoriteTicker>> GetFavoriteTicker(int id)
        {
            var favoriteTicker = await _context.FavoriteTickers.FindAsync(id);

            if (favoriteTicker == null)
            {
                return NotFound();
            }

            return favoriteTicker;
        }

        // PUT: api/FavoriteTickers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFavoriteTicker(int id, FavoriteTicker favoriteTicker)
        {
            if (id != favoriteTicker.Id)
            {
                return BadRequest();
            }

            _context.Entry(favoriteTicker).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FavoriteTickerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/FavoriteTickers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<FavoriteTicker>> PostFavoriteTicker(FavoriteTicker favoriteTicker)
        {
            _context.FavoriteTickers.Add(favoriteTicker);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFavoriteTicker", new { id = favoriteTicker.Id }, favoriteTicker);
        }

        // DELETE: api/FavoriteTickers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavoriteTicker(int id)
        {
            var favoriteTicker = await _context.FavoriteTickers.FindAsync(id);
            if (favoriteTicker == null)
            {
                return NotFound();
            }

            _context.FavoriteTickers.Remove(favoriteTicker);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FavoriteTickerExists(int id)
        {
            return _context.FavoriteTickers.Any(e => e.Id == id);
        }
    }
}
