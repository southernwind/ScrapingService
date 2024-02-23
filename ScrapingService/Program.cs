using System;
using System.Threading.Tasks;

using DataBase;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ScrapingService.Composition;
using ScrapingService.Targets;

namespace ScrapingService {
	public class Program {
		public static async Task Main() {
			var builder = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json", false, true);
			var configuration = builder.Build();

			var serviceProvider = new ServiceCollection()
				.AddLogging(x => {
					x.AddConfiguration(configuration.GetSection("Logging"));
					x.AddConsole();
				})
				.AddScoped<IConfiguration>(_ => configuration)
				.AddScoped<IScrapingServiceTarget, SbiSecInvestmentTrust>()
				.AddScoped<IScrapingServiceTarget, YahooFinance>()
				.AddScoped<IScrapingServiceTarget, Minkabu>()
				.AddScoped<IScrapingServiceTarget, YahooFinanceCurrency>()
				.AddDbContext<HomeServerDbContext>(optionsBuilder => {
					optionsBuilder.UseMySql(configuration.GetConnectionString("Database"),ServerVersion.AutoDetect(configuration.GetConnectionString("Database")));
				})
				.AddScoped<Cron>()
				.BuildServiceProvider();

			var isCanceled = false;

			var cron = serviceProvider.GetService<Cron>();

			if (cron == null) {
				throw new ApplicationException();
			}

			await cron.StartAsync();
			Console.CancelKeyPress += (_, _) => {
				cron.Dispose();
				isCanceled = true;
			};

			// 永久ループ
			while (!isCanceled) {
				Console.ReadLine();
			}
		}
	}
}
