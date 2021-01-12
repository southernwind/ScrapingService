using System;
using System.Threading.Tasks;

namespace ScrapingService.Composition {
	public interface IScrapingServiceTarget : IDisposable {
		/// <summary>
		/// スクレイピング実行
		/// </summary>
		public Task ExecuteAsync(int investmentProductId, string key);
	}
}
