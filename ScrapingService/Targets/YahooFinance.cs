using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Database.Tables;

using DataBase;

using Microsoft.EntityFrameworkCore;

namespace ScrapingService.Targets {
	public class YahooFinance : YahooFinanceBase {
		private readonly HomeServerDbContext _dbContext;

		public YahooFinance(HomeServerDbContext dbContext) {
			this._dbContext = dbContext;
		}

		public override async Task ExecuteAsync(int investmentProductId, string key) {
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
			this._dbContext.Database.ExecuteSqlRaw("SET sql_mode=''");
			var csv = await this.GetRecords(key);
			var records = csv.Where(x => x.AdjClose != null).Select(cr => new InvestmentProductRate {
				InvestmentProductId = investmentProductId,
				Date = cr.Date,
				Value = cr.AdjClose ?? 0
			}).ToArray();
			if (!records.Any()) {
				throw new Exception("取得件数0件");
			}

			var existing = (await this._dbContext
					.InvestmentProductRates
					.Where(x =>
						x.InvestmentProductId == investmentProductId)
					.ToArrayAsync())
				.Where(x =>
					x.Date <= records.Max(r => r.Date) &&
					x.Date >= records.Min(r => r.Date))
				.ToArray();

			this._dbContext.InvestmentProductRates.RemoveRange(existing);
			await this._dbContext.InvestmentProductRates.AddRangeAsync(records);
			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
		}
	}
}
