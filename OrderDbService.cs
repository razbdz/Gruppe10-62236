using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace PackBot.Data;


public class OrderDbService
{
   public async Task InitAsync()
   {
       await using var db = new PackBotDbContext();


       // Opret DB hvis den ikke findes
       await db.Database.EnsureCreatedAsync();


       // Hvis DB'en fandtes i forvejen (gammel schema), så mangler Accounts typisk.
       // EnsureCreated opgraderer IKKE → så vi tjekker og genskaber DB hvis Accounts mangler.
       var hasAccounts = await TableExistsAsync(db, "Accounts");
       if (!hasAccounts)
       {
           await db.Database.EnsureDeletedAsync();
           await db.Database.EnsureCreatedAsync();
       }


       // (valgfri) seed orders så I har noget at vise
       await SeedDemoAsync(force: false);
   }


   public async Task<int?> GetBagCountAsync(string orderId)
   {
       await using var db = new PackBotDbContext();
       var o = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId);
       return o?.BagCount;
   }


   public async Task UpsertOrderAsync(string orderId, int bagCount)
   {
       await using var db = new PackBotDbContext();


       var existing = await db.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
       if (existing is null)
       {
           db.Orders.Add(new Order { Id = orderId, BagCount = bagCount });
       }
       else
       {
           existing.BagCount = bagCount;
       }


       await db.SaveChangesAsync();
   }


   public async Task SeedDemoAsync(bool force)
   {
       await using var db = new PackBotDbContext();


       if (!force && await db.Orders.AnyAsync())
           return;


       db.Orders.AddRange(
           new Order { Id = "12345", BagCount = 4 },
           new Order { Id = "10452", BagCount = 7 },
           new Order { Id = "1001",  BagCount = 3 }
       );


       await db.SaveChangesAsync();
   }


   public async Task ResetAsync()
   {
       await using var db = new PackBotDbContext();
       await db.Database.EnsureDeletedAsync();
       await db.Database.EnsureCreatedAsync();
   }


   private static async Task<bool> TableExistsAsync(DbContext db, string tableName)
   {
       await using var conn = db.Database.GetDbConnection();
       if (conn.State != System.Data.ConnectionState.Open)
           await conn.OpenAsync();


       await using var cmd = conn.CreateCommand();
       cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name;";
       var p = cmd.CreateParameter();
       p.ParameterName = "$name";
       p.Value = tableName;
       cmd.Parameters.Add(p);


       var result = await cmd.ExecuteScalarAsync();
       return result != null && result != DBNull.Value;
   }
}


