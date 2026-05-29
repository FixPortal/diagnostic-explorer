#region Copyright

// Diagnostic Explorer, a .Net diagnostic toolset
// Copyright (C) 2010 Cameron Elliot
// 
// This file is part of Diagnostic Explorer.
// 
// Diagnostic Explorer is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Diagnostic Explorer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with Diagnostic Explorer.  If not, see <http://www.gnu.org/licenses/>.
// 
// http://diagexplorer.sourceforge.net/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace DiagnosticExplorer
{
	public class RateCounter
	{
		private static readonly Timer _timer;
		private static readonly List<WeakReference> _counters = new List<WeakReference>();
		private DateTime _lastCheck = DateTime.UtcNow;
		private readonly int[] _counts;
		private readonly TimeSpan[] _times;
		private int _index;
		public event EventHandler<RateSampleEventArgs> SampleCollected;

		static RateCounter()
		{
			_timer = new Timer();
			_timer.Elapsed += Run;
			_timer.Interval = 1000;
		}

		public RateCounter(int secondsAverage)
		{
			_counts = new int[secondsAverage];
			_times = new TimeSpan[secondsAverage];

			lock (_counters)
			{
				_counters.Add(new WeakReference(this));
				if (_counters.Count == 1)
					_timer.Start();
			}
		}


		private void Increment()
		{
			lock (_counts)
			{
				_times[_index % _counts.Length] = DateTime.UtcNow - _lastCheck;
				CalcRate();
				_index++;
				_counts[_index % _counts.Length] = 0;
				_times[_index % _counts.Length] = TimeSpan.Zero;
				_lastCheck = DateTime.UtcNow;

				InvokeSampleCollected();
			}
		}

		private void InvokeSampleCollected()
		{
			try
			{
				EventHandler<RateSampleEventArgs> sampleCollectedHandler = SampleCollected;

				if (sampleCollectedHandler != null)
				{
					RateSampleEventArgs args = new RateSampleEventArgs(Rate, GetRates(_times.Length));
					foreach (EventHandler<RateSampleEventArgs> handler in sampleCollectedHandler.GetInvocationList())
						// Delegate.BeginInvoke throws PlatformNotSupportedException on .NET Core/5+.
						// Task.Run gives the same fire-and-forget async dispatch portably.
						Task.Run(() => handler(this, args));
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		private void CalcRate()
		{
			double r = _counts.Sum();
			TimeSpan totalTime = _times.Aggregate((t1, t2) => t1 + t2);

			if (totalTime == TimeSpan.Zero)
				Rate = 0;
			else
				Rate = r / totalTime.TotalSeconds;
		}

		public void Register(int count)
		{
			lock (_counts)
			{
				if (count > 0)
					Total += (ulong) count;
				_counts[_index % _counts.Length] += count;
			}
		}

		/// <summary>
		/// Gets the last n seconds rates
		/// </summary>
		/// <param name="seconds">The number of seconds worth of data to fetch</param>
		/// <returns>A list of rates for the last n seconds, starting with the latest</returns>
		public int[] GetRates(int seconds)
		{
			lock (_counts)
			{
				return GetRates(seconds, _index, _counts);
			}
		}

		public static int[] GetRates(int seconds, int currentIndex, int[] values)
		{
			seconds = Math.Min(seconds, currentIndex);
			int[] results = new int[seconds];
			
			for (int i = 0; i < results.Length; i++)
					results[i] = values[(currentIndex - 1 - i) % values.Length];
			
			return results;
		}

		public double Rate { get; private set; }

		public ulong Total { get; private set; }

		private static void Run(object state, ElapsedEventArgs e)
		{
			try
			{
				lock (_counters)
				{
					for (int i = _counters.Count - 1; i >= 0; i--)
					{
						WeakReference r = _counters[i];
						RateCounter counter = (RateCounter) r.Target;
						if (counter == null)
							_counters.RemoveAt(i);
						else
							counter.Increment();
					}
					if (_counters.Count == 0)
						_timer.Stop();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
	}
}