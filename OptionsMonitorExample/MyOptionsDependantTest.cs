using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OptionsMonitorExample
{
	[TestClass]
	public class MyOptionsDependantTest
	{
		#region Fields

		private static readonly string _projectDirectoryPath = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
		private static readonly string _appSettingsJsonFilePath = Path.Combine(_projectDirectoryPath, "AppSettings.json");

		#endregion

		#region Methods

		private static async Task CreateConfigurationAsync()
		{
			var configurationBuilder = new ConfigurationBuilder();
			configurationBuilder.AddJsonFile("AppSettings.json", false, true);
			configurationBuilder.SetFileProvider(new PhysicalFileProvider(_projectDirectoryPath));
			return await Task.FromResult(configurationBuilder.Build());
		}

		private static async Task EnsureAppSettingsJsonFileIsDeletedAsync()
		{
			await Task.CompletedTask;

			if (File.Exists(_appSettingsJsonFilePath))
				File.Delete(_appSettingsJsonFilePath);
		}

		private static async Task Test(Action optionsConfigurationAction, Correctness correctness)
		{
			var configuration = await CreateConfigurationAsync();
			var services = new ServiceCollection();

			services.AddSingleton(configuration);
			services.AddSingleton();

			optionsConfigurationAction(configuration, services);

			var serviceProvider = services.BuildServiceProvider();

			var myOptionsDependant = serviceProvider.GetRequiredService();
			var myOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor>();

			Assert.IsFalse(myOptionsDependant.ChangeTimestamps.Any());

			if (correctness == Correctness.Full || correctness == Correctness.None)
			{
				Assert.AreEqual("First value", myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.AreEqual(string.Empty, myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.ThirdProperty);
			}
			else if (correctness == Correctness.Half)
			{
				Assert.IsNull(myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.ThirdProperty);
			}

			var updateContent = await File.ReadAllTextAsync(Path.Combine(_projectDirectoryPath, "AppSettings.Update.json"));
			await File.WriteAllTextAsync(_appSettingsJsonFilePath, updateContent);
			Thread.Sleep(1000);

			if (correctness == Correctness.None)
				Assert.IsFalse(myOptionsDependant.ChangeTimestamps.Any());
			else
				Assert.AreEqual(2, myOptionsDependant.ChangeTimestamps.Count);

			if (correctness == Correctness.Full)
			{
				Assert.AreEqual("First value", myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.AreEqual("Second value", myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.AreEqual("Third value", myOptionsMonitor.CurrentValue.ThirdProperty);
			}
			else if (correctness == Correctness.Half)
			{
				Assert.IsNull(myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.ThirdProperty);
			}
			else if (correctness == Correctness.None)
			{
				Assert.AreEqual("First value", myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.AreEqual(string.Empty, myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.ThirdProperty);
			}

			var resetContent = await File.ReadAllTextAsync(Path.Combine(_projectDirectoryPath, "AppSettings.Default.json"));
			await File.WriteAllTextAsync(_appSettingsJsonFilePath, resetContent);
			Thread.Sleep(1000);

			if (correctness == Correctness.None)
				Assert.IsFalse(myOptionsDependant.ChangeTimestamps.Any());
			else
				Assert.AreEqual(4, myOptionsDependant.ChangeTimestamps.Count);

			if (correctness == Correctness.Full || correctness == Correctness.None)
			{
				Assert.AreEqual("First value", myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.AreEqual(string.Empty, myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.ThirdProperty);
			}
			else if (correctness == Correctness.Half)
			{
				Assert.IsNull(myOptionsMonitor.CurrentValue.FirstProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.SecondProperty);
				Assert.IsNull(myOptionsMonitor.CurrentValue.ThirdProperty);
			}
		}

		[TestCleanup]
		public async Task TestCleanup()
		{
			await EnsureAppSettingsJsonFileIsDeletedAsync();
		}

		[TestInitialize]
		public async Task TestInitialize()
		{
			await EnsureAppSettingsJsonFileIsDeletedAsync();

			File.Copy(Path.Combine(_projectDirectoryPath, "AppSettings.Default.json"), _appSettingsJsonFilePath);
		}

		[TestMethod]
		public async Task ThisIsCorrect()
		{
			await Test((configuration, services) =>
			{
				// -------------------------------------------------------------
				services.Configure(configuration.GetSection(nameof(MyOptions)));
				// -------------------------------------------------------------
			}, Correctness.Full);
		}

		[TestMethod]
		public async Task ThisIsNotCorrect()
		{
			await Test((configuration, services) =>
			{
				// --------------------------------------------------------------------------------------
				services.Configure(options => configuration.GetSection(nameof(MyOptions)).Bind(options));
				// --------------------------------------------------------------------------------------
			}, Correctness.None);
		}

		[TestMethod]
		public async Task ThisIsOnlyHalfCorrect()
		{
			await Test((configuration, services) =>
			{
				// -------------------------------
				services.Configure(configuration);
				// -------------------------------
			}, Correctness.Half);
		}

		#endregion
	}
}
