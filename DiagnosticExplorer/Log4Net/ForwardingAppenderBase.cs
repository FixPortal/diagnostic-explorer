using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using log4net.Appender;

namespace DiagnosticExplorer.Log4Net
{

	[DiagnosticClass(AttributedPropertiesOnly = true, DeclaringTypeOnly = false)]
	public abstract class ForwardingAppenderBase : log4net.Appender.ForwardingAppender
	{
		protected ForwardingAppenderBase()
		{
		}

		/// <summary>
		/// Wraps the appenders in the corresponding <see cref="AppenderProxy"/>
		/// </summary>
		public override void ActivateOptions()
		{
			base.ActivateOptions();

			if (FailTimeout < TimeSpan.Zero)
				FailTimeout = Appenders.Count == 1 ? TimeSpan.Zero : TimeSpan.FromMinutes(5);

			Proxies = Appenders.Cast<IAppender>().Select(a => new AppenderProxy(a, FailTimeout)).ToList();
			DiagnosticManager.Register(this, Name, "Log4Net");
		}

		public static void LogLogError(Type type, string msg, Exception exception = null)
		{
			// Route to log4net's internal log so appender/forwarding/async failures are visible
			// in Release. Previously this ignored msg and only Debug.WriteLine'd a non-null
			// exception, so dropped logs and failover problems left no trace.
			Debug.WriteLine($"{msg} {exception}");
			log4net.Util.LogLog.Error(type, msg, exception);
		}

		[RateProperty(ExposeRate = false, ExposeTotal =  true)]
		public RateCounter EventsIn { get; set; } = new RateCounter(3);

		[RateProperty(ExposeRate = false, ExposeTotal = true)]
		public RateCounter EventsOut { get; set;  } = new RateCounter(3);

		[RateProperty(ExposeRate = false, ExposeTotal = true)]
		public RateCounter EventsErrored { get; set;  } = new RateCounter(3);

		[Property]
		public string Type => GetType().Name;

		[CollectionProperty(CollectionMode.Categories, CategoryProperty= nameof(AppenderProxy.Name))]
		public List<AppenderProxy> Proxies { get; private set; }

		/// <summary>Used to specify the amount of minutes timeout to wait for before resetting that an error occurred on an appender.</summary>
		[Property]
		public TimeSpan FailTimeout { get; set; } = TimeSpan.FromSeconds(-1);

		protected override void OnClose()
		{
			base.OnClose();
			DiagnosticManager.Unregister(this);
		}

	}
}