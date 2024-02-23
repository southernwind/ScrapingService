using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Database.Tables;

using DataBase;

using HtmlAgilityPack.CssSelectors.NetCore;

using Microsoft.EntityFrameworkCore;

using ScrapingLibrary;

using ScrapingService.Composition;

namespace ScrapingService.Targets {
	public class Minkabu : IScrapingServiceTarget {
		private readonly HttpClientWrapper _httpClient;
		private readonly HomeServerDbContext _dbContext;

		public Minkabu(HomeServerDbContext dbContext) {
			this._httpClient = new HttpClientWrapper();
			this._dbContext = dbContext;
		}

		public async Task ExecuteAsync(int investmentProductId, string key) {
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
			this._dbContext.Database.ExecuteSqlRaw("SET sql_mode=''");
			var url = $"https://itf.minkabu.jp/fund/{key}/get_line_daily_json";
			var json = await this._httpClient.GetAsync(url).ToJsonAsync();
			var rec = json.data;
			var records = new List<InvestmentProductRate>();

			foreach (var record in json.data) {
				var rate = new InvestmentProductRate {
					InvestmentProductId = investmentProductId,
					Date = DateTimeOffset.FromUnixTimeMilliseconds((long)record[0]).LocalDateTime,
					Value = record[4]
				};
				records.Add(rate);
			}

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

		protected virtual void Dispose(bool disposing) {
		}

		void IDisposable.Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
