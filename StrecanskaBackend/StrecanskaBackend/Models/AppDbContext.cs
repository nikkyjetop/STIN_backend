using Microsoft.EntityFrameworkCore;
using StrecanskaBackend.Models.Tables;
using System.Collections.Generic;

namespace StrecanskaBackend.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<FavoriteTicker> FavoriteTickers { get; set; } = null;
        public DbSet<StockData> StockDatas { get; set; } = null;
    }
}
