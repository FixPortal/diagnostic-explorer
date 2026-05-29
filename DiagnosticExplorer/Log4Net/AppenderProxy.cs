using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using DiagnosticExplorer.Util;
using log4net.Appender;
using log4net.Core;

namespace DiagnosticExplorer.Log4Net
{
	public struct AppendResult
	{
		public AppendResult(bool success) : this()
		{
			Success = success;
		}

		public AppendResult(bool success, string message)
		{
			Success = success;
			Message = message;
		}

		public bool Success { get; }
		public string Message { get; }
	}

	[DiagnosticClass(AttributedPropertiesOnly = true)]
	public abstract class AppenderProxyBase
	{
		protected TimeSpan _timeout;
		protected DateTime? _errorTime;


		public AppenderProxyBase(TimeSpan timeout)
		{
			_timeout = timeout;
		}

		public bool IsInError { get; protected set; }

		[Property]
		public DateTime? LastError { get; set; }

		[Property]
		public DateTime? LastMessageSent { get; set; }

		[RateProperty(ExposeRate = false, ExposeTotal = true)]
		public RateCounter MessagesSent { get; } = new RateCounter(3);

		[RateProperty(ExposeRate = false, ExposeTotal = true)]
		public RateCounter Errors { get; } = new RateCounter(3);

		[DiagnosticMethod]
		public void Reactivate()
		{
			IsInError = false;
			_errorTime = null;
		}

		[Property]
		public string LastErrorMessage { get; set; }

		private TimeSpan? TimeUntilNextActive()
		{
			DateTime? time = _errorTime;

			if (!time.HasValue)
				return null;

			TimeSpan elapsed = SystemDateTime.UtcNow() - time.Value;
			if (elapsed > _timeout)
				return null;

			return elapsed;
		}

		[Property]
		public string StatusMessage
		{
			get
			{
				TimeSpan? timeFailed = TimeUntilNextActive();
				if (timeFailed.HasValue)
				{
					string remaining = FormatTimeSpan(_timeout - timeFailed.Value);
					return $"FAILED, Ready in {remaining}";
				}
				return "READY";
			}
		}

		private string FormatTimeSpan(TimeSpan time)
		{
			if (time.TotalMinutes >= 60)
				return string.Format("{0:D2}:{1:D2}:{2:D2}", (int) time.TotalHours, time.Minutes, time.Seconds);

			if (time.TotalSeconds < 60)
				return string.Format("{0} seconds", time.Seconds);

			return string.Format("{0}m {1:D2}s", (int) time.TotalMinutes, time.Seconds);
		}

		protected bool DoAppend(Func<AppendResult> appendAction)
		{
			if (ShouldResetError())
			{
				_errorTime = null;
				IsInError = false;
			}

			if (IsInError)
				return false;

			AppendResult result = appendAction();
			if (result.Success)
			{
				LastMessageSent = SystemDateTime.UtcNow();
				MessagesSent.Register(1);
			}
			else
			{
				LastError = SystemDateTime.UtcNow();
				LastErrorMessage = result.Message;
				Errors.Register(1);
				if (_timeout > TimeSpan.Zero)
				{
					_errorTime = SystemDateTime.UtcNow();
					// Engage the fail-timeout quarantine. Without this the IsInError guard
					// above never trips, ShouldResetError never runs, and a dead appender is
					// retried on every event ("READY" forever).
					IsInError = true;
				}
			}

			return result.Success;
		}

		private bool ShouldResetError()
		{
			if (!IsInError)
				return false;

			if (_timeout <= TimeSpan.Zero)
				return true;

			return (SystemDateTime.UtcNow() - _errorTime) >= _timeout;
		}
	}



	[DiagnosticClass(AttributedPropertiesOnly = true)]
	public class SmtpAppenderProxy : AppenderProxyBase
	{
		private SmtpAppender _appender;

		public SmtpAppenderProxy(SmtpAppender appender, string smtpHost, TimeSpan timeout) : base(timeout)
		{
			_appender = appender;
			SmtpHost = smtpHost;
		}

		public string SmtpHost { get; set; }
		
		public bool TrySend(MailMessage message)
		{
			return DoAppend(() => SendMessage(message));
		}

		private AppendResult SendMessage(MailMessage message)
		{
			try
			{
				using SmtpClient smtpClient = new SmtpClient();
				if (!string.IsNullOrEmpty(SmtpHost) && !string.Equals(SmtpHost, SmtpAppender.DefaultHostName, StringComparison.CurrentCultureIgnoreCase))
					smtpClient.Host = SmtpHost;

				smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

				if (_appender.Authentication == log4net.Appender.SmtpAppender.SmtpAuthentication.Basic)
				{
					// Perform basic authentication
					smtpClient.Credentials = new NetworkCredential(_appender.Username, _appender.Password);
				}
				else if (_appender.Authentication == log4net.Appender.SmtpAppender.SmtpAuthentication.Ntlm)
				{
					// Perform integrated authentication (NTLM)
					smtpClient.Credentials = CredentialCache.DefaultNetworkCredentials;
				}
				smtpClient.Send(message);
				return new AppendResult(true);
			}
			catch (Exception ex)
			{
				return new AppendResult(false, ex.Message);
			}
		}
	}

	[DiagnosticClass(AttributedPropertiesOnly = true)]
	public class AppenderProxy : AppenderProxyBase
	{
		/// <summary>
		/// Wraps up an <see cref="IAppender"/> adding extra behaviour to how to handle
		/// an error while appending
		/// </summary>
		/// <param name="timeout">Amount of minutes to wait before attempting to append again while has error</param>
		public AppenderProxy(IAppender appenderToWrap, TimeSpan timeout) : base(timeout)
		{
			AppenderSkeleton convertedAppender = appenderToWrap as AppenderSkeleton;
			if (convertedAppender == null)
				throw new InvalidOperationException("Cannot use AppenderProxy with an appender that does not inherit from AppenderSkeleton as it needs to hook into the IErrorHandler, to gather errors.");

			Appender = convertedAppender;

			ErrorHandler = new AppenderProxyErrorHandler();
			MultiErrorHandler.SetErrorHandler(Appender, ErrorHandler);
		}

		private AppenderProxyErrorHandler ErrorHandler { get; }

		/// <summary>
		/// Attempts to append to wrapped appender
		/// </summary>
		/// <returns>Whether the append was successful</returns>
		public bool TryAppend(LoggingEvent loggingEvent)
		{
			return DoAppend(() => FireAppendAction(() => Appender.DoAppend(loggingEvent)));
		}

		/// <summary>
		/// Attempts to append to wrapped appender
		/// </summary>
		/// <returns>Whether the append was successful</returns>
		public bool TryAppend(LoggingEvent[] loggingEvents)
		{
			return DoAppend(() => FireAppendAction(() => Appender.DoAppend(loggingEvents)));
		}

		private AppendResult FireAppendAction(Action appendAction)
		{
			ErrorHandler.EnableForCurrentThread();
			ErrorHandler.ResetError();
			try
			{
				appendAction();
			}
			finally
			{
				ErrorHandler.Disable();
			}
			return new AppendResult(!ErrorHandler.HasError, ErrorHandler.Message);
		}

		/// <summary>
		/// Appender being wrapped
		/// </summary>
		public AppenderSkeleton Appender { get; }

		public string Name => Appender.Name;
	}
}