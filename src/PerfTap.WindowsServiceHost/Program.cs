﻿namespace PerfTap.WindowsServiceHost
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.ServiceProcess;
	using System.Threading;
	using NLog;
	using NanoTube.Configuration;
	using PerfTap.Configuration;
	using ServiceChassis;
	using System.Configuration.Install;

	public class Program
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				_log.Info(() => "PerfTap Started");
				if (Environment.UserInteractive)
				{
					string action = args.Length >= 1 ? args[0] : "";

					switch (action)
					{
						case "--install":
							InstallService();
							break;
						case "--uninstall":
							UninstallService();
							break;
						case "--version":
							PrintVersion();
							break;
						case "--console":
							RunConsoleMode();
							break;
						case "--help":
							PrintHelp();
							break;

						default:
#if DEBUG
							if (global::System.Diagnostics.Debugger.IsAttached)
							{
								RunConsoleMode();
							}
#endif

							PrintHelp(action);
							Environment.Exit(1);
							break;
					}
					Environment.Exit(0);
				}
				else
				{
					ServiceBase.Run(new TaskService(cancellation => new MonitoringTaskFactory(CounterSamplingConfiguration.FromConfig(),
						MetricPublishingConfiguration.FromConfig()).CreateContinuousTask(cancellation)));
				}
			}
			catch (Exception ex)
			{
				_log.FatalException(String.Format("An unhandled error occurred in the PerfTap Service on [{0}]",
				Environment.MachineName), ex);
				throw;
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_log.FatalException(String.Format("An unhandled error occurred in the PerfTap Service on [{0}]",
			Environment.MachineName), e.ExceptionObject as Exception);
		}
		private static void PrintHelp(string action = null)
		{
			Action<string> C = (input) => { Console.WriteLine(input); };
			if (action != null)
			{
				C("Error - unknown option: " + action);
			}
			C("Usage: PerfTap [ --install | --uninstall | --console | --version | --help ]");
			C("  --install              Install statsd.net as a Windows Service.");
			C("  --uninstall            Uninstall statsd.net");
			C("  --console              Run statsd.net in console mode (does not need to be installed first)");
			C("  --version              Prints the service version");
			C("  --help                 Prints this help information.");
		}

		private static void InstallService()
		{
			try
			{
				ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
				Console.WriteLine("Service installed successfully (don't forget to start it!)");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Could not install the service: " + ex.Message);
				Console.WriteLine(ex.ToString());
			}
		}

		private static void UninstallService()
		{
			try
			{
				ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
				Console.WriteLine("Service uninstalled successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Could not uninstall the service: " + ex.Message);
				Console.WriteLine(ex.ToString());
			}
		}

		private static void PrintVersion()
		{
			Console.WriteLine("PerfTap v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
		}

		private static void RunConsoleMode()
		{
			// Debug code: this allows the process to run as a non-service.
			// It will kick off the service start point, but never kill it.
			// Shut down the debugger to exit
			//TODO: this factory needs to be registered in a container to make this more general purpose 
			using (var service = new TaskService(cancellation => new MonitoringTaskFactory(CounterSamplingConfiguration.FromConfig(), MetricPublishingConfiguration.FromConfig()).CreateContinuousTask(cancellation)))
			{
				// Put a breakpoint in OnStart to catch it
				typeof(CustomServiceBase).GetMethod("OnStart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(service, new object[] { null });
				//make sure we don't release the instance yet ;0
				Thread.Sleep(Timeout.Infinite);
			}
		}
	}
}