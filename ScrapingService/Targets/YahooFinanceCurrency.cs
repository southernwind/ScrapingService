using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Database.Tables;

using DataBase;

using Microsoft.EntityFrameworkCore;

namespace ScrapingService.Targets {
	public class YahooFinanceCurrency : YahooFinanceBase {
		private readonly HomeServerDbContext _dbContext;

		public YahooFinanceCurrency(HomeServerDbContext dbContext) {
			this._dbContext = dbContext;
		}

		public override async Task ExecuteAsync(int id, string key) {
			var csv = await this.GetRecords(key);
			var records = csv.Select(cr => new InvestmentCurrencyRate {
				InvestmentCurrencyUnitId = id,
				Date = cr.Date,
				Value = cr.AdjClose
			}).ToArray();
			if (!records.Any()) {
				throw new Exception("取得件数0件");
			}

			var existing = (await this._dbContext
					.InvestmentCurrencyRates
					.Where(x =>
						x.InvestmentCurrencyUnitId == id)
					.ToArrayAsync())
				.Where(x =>
					x.Date <= records.Max(r => r.Date) &&
					x.Date >= records.Min(r => r.Date))
				.ToArray();

			await this._dbContext.InvestmentCurrencyRates.AddRangeAsync(records.Except(records.Where(x => existing.Any(e => e.InvestmentCurrencyUnitId == x.InvestmentCurrencyUnitId && e.Date == x.Date))));
			await this._dbContext.SaveChangesAsync();
		}
	}
}
