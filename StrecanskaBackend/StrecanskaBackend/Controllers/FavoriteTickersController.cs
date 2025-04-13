﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            FavoriteTicker? favoriteTicker = await _context.FavoriteTickers.FindAsync(id);

            if (favoriteTicker == null)
            {
                return NotFound();
            }

            return favoriteTicker;
        }

        // DELETE: api/FavoriteTickers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavoriteTicker(int id)
        {
            FavoriteTicker? favoriteTicker = await _context.FavoriteTickers.FindAsync(id);
            if (favoriteTicker == null)
            {
                return NotFound();
            }

            List<StockData> relatedStockData = await _context.StockDatas
                .Where(sd => sd.FavoriteTickers_id == id)
                .ToListAsync();

            if (relatedStockData.Any())
            {
                _context.StockDatas.RemoveRange(relatedStockData);
                await _context.SaveChangesAsync();
            }

            _context.FavoriteTickers.Remove(favoriteTicker);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpPost]
        public async Task<ActionResult<FavoriteTicker>> PostFavoriteTicker([FromBody] string favoriteTicker)
        {
            if (string.IsNullOrWhiteSpace(favoriteTicker))
            {
                return BadRequest("Ticker is empty.");
            }

            HttpClient client = new();
            string profileUrl = $"https://finnhub.io/api/v1/stock/profile2?symbol={favoriteTicker}&token=" + AppDbContext.API_KEY;

            HttpResponseMessage profileResponse = await client.GetAsync(profileUrl);
            if (!profileResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)profileResponse.StatusCode, "Error communicating with Finnhub API.");
            }

            string profileJson = await profileResponse.Content.ReadAsStringAsync();
            ProfileResponse? profile = JsonSerializer.Deserialize<ProfileResponse>(profileJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
            {
                return BadRequest("Ticker " + favoriteTicker + " not found.");
            }

            bool exists = await _context.FavoriteTickers.AnyAsync(x => x.Ticker == favoriteTicker.ToUpper());
            if (exists)
            {
                return Conflict("Ticker is already in favorites.");
            }

            FavoriteTicker newFavorite = new()
            {
                Ticker = favoriteTicker.ToUpper(),
                Name = profile.Name,
                Logo = profile.Logo
            };

            _context.FavoriteTickers.Add(newFavorite);
            await _context.SaveChangesAsync();

            string quoteUrl = $"https://finnhub.io/api/v1/quote?symbol={newFavorite.Ticker}&token=" + AppDbContext.API_KEY;
            HttpResponseMessage quoteResponse = await client.GetAsync(quoteUrl);

            if (quoteResponse.IsSuccessStatusCode)
            {
                string quoteJson = await quoteResponse.Content.ReadAsStringAsync();
                QuoteResponse? quote = JsonSerializer.Deserialize<QuoteResponse>(quoteJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quote != null && quote.C != 0)
                {
                    StockData newStockData = new()
                    {
                        Price = quote.C,
                        Date = DateTime.UtcNow,
                        FavoriteTickers_id = newFavorite.Id
                    };

                    _context.StockDatas.Add(newStockData);
                    await _context.SaveChangesAsync();
                }
            }

            return CreatedAtAction("GetFavoriteTicker", new { id = newFavorite.Id }, newFavorite);
        }


        [HttpGet("FilteredPrices")]
        public async Task<ActionResult<IEnumerable<TickerWithPrice>>> GetFilteredPrices([FromQuery] int filterId = 1)
        {

            List<FavoriteTicker> tickers = await _context.FavoriteTickers.ToListAsync();
            List<TickerWithPrice> result = new();
            Console.WriteLine("Filter ID: " + filterId);

            switch (filterId)
            {
                case 1:
                    foreach (FavoriteTicker? ft in tickers)
                    {
                        StockData? latestPrice = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.Id)
                            .OrderByDescending(sd => sd.Date)
                            .FirstOrDefaultAsync();

                        result.Add(new TickerWithPrice
                        {
                            Ticker = ft.Ticker,
                            Name = ft.Name,
                            Logo = ft.Logo,
                            LatestPrice = latestPrice?.Price,
                            LatestDate = latestPrice?.Date
                        });
                    }
                    break;

                case 2:
                    DateTime today = DateTime.UtcNow.Date;
                    DateTime threeDaysAgo = today.AddDays(-3);

                    foreach (FavoriteTicker? ft in tickers)
                    {
                        List<StockData> prices = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.Id && sd.Date >= threeDaysAgo)
                            .ToListAsync();

                        List<StockData> grouped = prices
                            .GroupBy(p => p.Date.Date)
                            .Select(g => g.OrderByDescending(p => p.Date).First())
                            .OrderByDescending(p => p.Date)
                            .Take(3)
                            .ToList();

                        if (grouped.Count < 3)
                            continue;

                        if (grouped[0].Price >= grouped[1].Price && grouped[1].Price >= grouped[2].Price)
                        {
                            result.Add(new TickerWithPrice
                            {
                                Ticker = ft.Ticker,
                                Name = ft.Name,
                                Logo = ft.Logo,
                                LatestPrice = grouped[0].Price,
                                LatestDate = grouped[0].Date
                            });
                        }
                    }
                    break;

                case 3:
                    DateTime now = DateTime.UtcNow;
                    DateTime fiveDaysAgo = now.Date.AddDays(-5);

                    foreach (FavoriteTicker? ft in tickers)
                    {
                        List<StockData> prices = await _context.StockDatas
                            .Where(sd => sd.FavoriteTickers_id == ft.Id && sd.Date >= fiveDaysAgo)
                            .ToListAsync();

                        List<StockData> grouped = prices
                            .GroupBy(p => p.Date.Date)
                            .Select(g => g.OrderByDescending(p => p.Date).First())
                            .OrderBy(p => p.Date)
                            .ToList();

                        if (grouped.Count < 3)
                            continue;

                        int declineCount = 0;
                        for (int i = 1; i < grouped.Count; i++)
                        {
                            if (grouped[i].Price < grouped[i - 1].Price)
                                declineCount++;
                        }

                        if (declineCount <= 2)
                        {
                            StockData latest = grouped.Last();
                            result.Add(new TickerWithPrice
                            {
                                Ticker = ft.Ticker,
                                Name = ft.Name,
                                Logo = ft.Logo,
                                LatestPrice = latest.Price,
                                LatestDate = latest.Date
                            });
                        }
                    }
                    break;

                default:
                    return BadRequest("Neznámý filtr.");
            }

            return Ok(result);
        }


        [HttpPost("Rating")]
        public async Task<ActionResult<List<TickerWithRating>>> GetRatings([FromBody] List<string> tickers)
        {
            /*if (tickers == null || !tickers.Any())
                return BadRequest("Seznam tickerů je prázdný.");

            var requestPayload = new
            {
                stocks = tickers.Select(t => new { symbol = t }).ToList()
            };

            HttpClient httpClient = new();
            string requestJson = JsonSerializer.Serialize(requestPayload);
            StringContent content = new(requestJson, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync("https://stin-2025-app-05a64c392b3e.herokuapp.com/evaluateStocks", content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Chyba při volání externí služby: {ex.Message}");
            }

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Nepodařilo se získat hodnocení tickerů.");

            string responseContent = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseContent);
            JsonElement stockRatings = doc.RootElement.GetProperty("data").GetProperty("stocks");

            List<FavoriteTicker> dbTickers = await _context.FavoriteTickers
                .Where(ft => tickers.Contains(ft.Ticker))
                .ToListAsync();

            List<TickerWithRating> result = new();
            foreach (JsonElement stock in stockRatings.EnumerateArray())
            {
                string symbol = stock.GetProperty("symbol").GetString() ?? "";
                string ratingStr = stock.GetProperty("rating").GetString() ?? "0";

                if (!int.TryParse(ratingStr.Split('.')[0], out int rating))
                    rating = 0;

                FavoriteTicker? match = dbTickers.FirstOrDefault(ft => ft.Ticker == symbol);
                if (match != null)
                {
                    result.Add(new TickerWithRating
                    {
                        Ticker = match.Ticker,
                        Name = match.Name,
                        Logo = match.Logo,
                        Rating = rating
                    });
                }
            }

            return Ok(result);*/


            if (tickers == null || !tickers.Any())
                return BadRequest("Seznam tickerů je prázdný.");

            Random random = new();

            IQueryable<FavoriteTicker> favoriteTickers = _context.FavoriteTickers.Where(x => tickers.Contains(x.Ticker));

            List<TickerWithRating> tickerRatings = favoriteTickers.Select(x => new TickerWithRating
            {

                Ticker = x.Ticker,
                Name = x.Name,
                Logo = x.Logo,
                Rating = random.Next(-10, 11)
            }).ToList();

            return Ok(tickerRatings);
        }


        [HttpPost("ProcessTickers")]
        public async Task<ActionResult<List<TickerWithRecommendation>>> ProcessTickers([FromBody] List<TickerWithRating> tickers, [FromQuery] int tickerLimit)
        {
            /*if (tickers == null || !tickers.Any())
                return BadRequest("Seznam tickerů je prázdný.");

            List<TickerWithRecommendation> filteredProfiles = tickers
                .Select(t => new TickerWithRecommendation
                {
                    Ticker = t.Ticker,
                    Name = t.Name,
                    Logo = t.Logo,
                    Recommendation = t.Rating >= tickerLimit ? Recommendation.Sell : Recommendation.Buy,
                })
                .ToList();

            var recommendationPayload = new
            {
                stocks = filteredProfiles.Select(p => new
                {
                    symbol = p.Ticker,
                    recommendation = p.Recommendation.ToString().ToUpper()
                })
            };

            HttpClient httpClient = new();
            string json = JsonSerializer.Serialize(recommendationPayload);
            StringContent content = new(json, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync("https://stin-2025-app-05a64c392b3e.herokuapp.com/recommendation", content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Chyba při volání externího API: {ex.Message}");
            }

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Nepodařilo se odeslat doporučení.");

            return Ok(filteredProfiles);*/




            if (tickers == null || !tickers.Any())
                return BadRequest("Seznam tickerů je prázdný.");

            List<TickerWithRecommendation> filteredProfiles = tickers
                .Select(t => new TickerWithRecommendation
                {
                    Ticker = t.Ticker,
                    Name = t.Name,
                    Logo = t.Logo,
                    Recommendation = t.Rating >= tickerLimit ? Recommendation.Sell : Recommendation.Buy,
                })
                .ToList();


            return Ok(filteredProfiles);
        }

    }
}
