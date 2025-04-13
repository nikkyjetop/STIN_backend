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
    public class StockDatasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockDatasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StockDatas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockData>>> GetStockDatas()
        {
            return await _context.StockDatas.ToListAsync();
        }

        // GET: api/StockDatas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StockData>> GetStockData(int id)
        {
            var stockData = await _context.StockDatas.FindAsync(id);

            if (stockData == null)
            {
                return NotFound();
            }

            return stockData;
        }


        // DELETE: api/StockDatas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockData(int id)
        {
            var stockData = await _context.StockDatas.FindAsync(id);
            if (stockData == null)
            {
                return NotFound();
            }

            _context.StockDatas.Remove(stockData);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
