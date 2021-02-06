using System;
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
	public class SbiSecInvestmentTrust : IScrapingServiceTarget {
		private readonly HttpClientWrapper _httpClient;
		private readonly HomeServerDbContext _dbContext;

		public SbiSecInvestmentTrust(HomeServerDbContext dbContext) {
			this._httpClient = new HttpClientWrapper();
			this._dbContext = dbContext;
		}

		public async Task ExecuteAsync(int investmentProductId, string key) {
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
			var url = $"https://site0.sbisec.co.jp/marble/fund/history/standardprice.do?fund_sec_code={key}";
			var htmlDoc = await this._httpClient.GetAsync(url).ToHtmlDocumentAsync();
			var trs = htmlDoc.DocumentNode.QuerySelectorAll("#main .mgt10 .accTbl01 table tbody tr");
			var records = trs.Select(tr => new InvestmentProductRate {
				InvestmentProductId = investmentProductId,
				Date = DateTime.Parse(tr.QuerySelector("th").InnerText),
				Value = int.Parse(tr.QuerySelectorAll("td").First().InnerText.Replace("円", "").Replace(",", ""))
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

		protected virtual void Dispose(bool disposing) {
		}

		void IDisposable.Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
