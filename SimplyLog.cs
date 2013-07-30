/*	SimplyLog
 *	Copyright (c) 2013, Taylor Spencer <taylorspencer@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 */

using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SimplyLog
{
	public class Logging
	{
		private LogFormat	logFormat;
		private LogLevel 	logLevel;
		private LogLevel	defaultLogLevel;
		private string		s_LogFile;

		// Mutex for file IO handle 
		private static Mutex	mutex		= new Mutex();
		private static bool		hasHandle	= false;

		/// <summary>
		/// Enumeration of available log formats: Console, Text, XHTML.
		/// </summary>
		/// <param name="CONSOLE">Log will be written to the local console.</param>
		/// <param name="TEXT">Log will be written to the specified local file.</param>
		/// <param name="XHTML">Log will be written to the specified local file in XHTML format.</param>
		public enum LogFormat
		{
			CONSOLE,
			TEXT,
			XHTML
		};

		/// <summary>
		/// Enumeration of available log levels.
		/// These are enumerated bit flags and can have bitwise operations performed on them.
		/// This allows multiple variations of acceptable levels to be set.
		/// </summary>
		/// <param name="NONE">Log nothing.</param>
		/// <param name="EXCEPTION">Log exceptions.</param>
		/// <param name="ERROR">Log errors.</param>
		/// <param name="WARNING">Log warnings.</param>
		/// <param name="INFO">Log informational entries.</param>
		/// <param name="CUSTOM">Log custom entries.</param>
		/// <param name="DEBUG">Log debug entries.</param>
		/// <param name="ALL">Log everything except debug.</param>
		[Flags]
		public enum LogLevel
		{
			NONE		= 0x0,	// 0
			EXCEPTION	= 0x1,	// 1
			ERROR	 	= 0x2,	// 2
			WARNING		= 0x4,	// 4
			INFO		= 0x8,	// 8
			CUSTOM		= 0x10,	// 16
			DEBUG		= 0x20,	// 32
			ALL			= 0x40	// 64
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="SimplyLog.Logging"/> class.
		/// Sets default values for log level, default log level and log file.
		/// </summary>
		public Logging()
		{
			// Assign initial safety values
			logLevel 		= LogLevel.EXCEPTION | LogLevel.ERROR;
			defaultLogLevel = LogLevel.INFO;
			s_LogFile		= "log.txt";
		}

		/// <summary>
		/// Log single formatted string at the default log level.
		/// </summary>
		/// <param name="s_Text">String to log.</param>
		public void Log( string s_Text )
		{
			Log( s_Text, this.DefaultLevel );
		}

		/// <summary>
		/// Log the specified string, loglevel and exception type if any.
		/// </summary>
		/// <param name="s_Text">String to log.</param>
		/// <param name="level">Log level.</param>
		/// <param name="e">Exception (optional).</param>
		public void Log( string s_Text, LogLevel level, Exception e = null  )
		{
			// Check if log level matches list set, ignore request if not
			if (( level & this.Level ) == 0 ){
				return;
			}
			// If message level was ALL then log at default level
			if ( level == LogLevel.ALL ){
				level = this.DefaultLevel;
			}

			// Prepare a formatted date/time/level string
			// Include calling thread id for DEBUG messages
			string s_DateTime	= DateTime.Now.ToString(); 
			string s_LogInfo	= "";

			switch( level ){
				case(LogLevel.EXCEPTION):
					this.LogException( e, s_Text );
					return;
				case(LogLevel.ERROR):
					s_LogInfo = s_DateTime + " ERROR: ";
					break;
				case(LogLevel.WARNING):
					s_LogInfo = s_DateTime + " WARNING: ";
					break;
				case(LogLevel.INFO):
					s_LogInfo = s_DateTime + " INFO: ";
					break;
				case(LogLevel.CUSTOM):
					s_LogInfo = s_DateTime + " CUSTOM: ";
					break;
				case(LogLevel.DEBUG):
					s_LogInfo = 
						s_DateTime + " DEBUG: " + "thread " +
						Thread.CurrentThread.ManagedThreadId + "(" +
						Thread.CurrentThread.Name + "): ";
					break;
				case(LogLevel.ALL):
					s_LogInfo = s_DateTime + " ALL: ";
					break;
				case(LogLevel.NONE):
					return;
				default:
					break;
			}

			// Check format and ensure properly formatted output, then write
			string s_formattedText;
			switch( this.Format ){
				case(LogFormat.TEXT):
					s_formattedText = s_LogInfo + s_Text;
					this.WriteToFile( s_formattedText );
					break;
				case(LogFormat.XHTML):
					s_formattedText = "<br>" + s_LogInfo + s_Text + "<br/>";
					this.WriteToFile( s_formattedText );
					break;
				default:
					s_formattedText = s_LogInfo + s_Text;
					this.WriteToConsole( s_formattedText );
					break;
			}
		}

		/// <summary>
		/// Log multiple formatted strings at the default loglevel
		/// </summary>
		/// <param name="s_ParamsText">String to log.</param>
		public void LogMulti( params string[] s_ParamsText )
		{
			LogLevel defaultLevel = this.DefaultLevel;

			foreach ( string _text in s_ParamsText ) {
				this.Log( _text, defaultLevel );
			}
		}

		/// <summary>
		/// Gets or sets the log format.
		/// </summary>
		public LogFormat Format
		{
			get { 
				return logFormat;
			}

			set {
				logFormat = value;
			}
		}

		/// <summary>
		/// Gets or sets the loglevel.
		/// If set to ALL then set all levels active except DEBUG
		/// If DEBUG is required then this level should be explicitly set.
		/// </summary>
		public LogLevel Level
		{
			get {
				return logLevel; 
			}

			set {
				if ( value.HasFlag( LogLevel.ALL ) ) {
					logLevel =
						LogLevel.EXCEPTION	| 
						LogLevel.ERROR		| 
						LogLevel.WARNING	| 
						LogLevel.INFO		| 
						LogLevel.CUSTOM;

					if ( value.HasFlag( LogLevel.DEBUG ) ) {
						// DEBUG level was explicitly set, append to existing flags
						logLevel = logLevel | LogLevel.DEBUG;
					}
					return;
				}
				logLevel = value;
			}
		}

		/// <summary>
		/// Gets or sets the default log level.
		/// </summary>
		public LogLevel DefaultLevel
		{
			get {
				return defaultLogLevel; 
			}

			set {
				defaultLogLevel = value;
			}
		}

		/// <summary>
		/// Gets or sets the log filename.
		/// </summary>
		public string File
		{
			get {
				return s_LogFile; 
			}

			set {
				s_LogFile = value;
			}
		}

		/// <summary>
		/// Writes the log header in simplified XHTML format
		/// </summary>
		/// <param name="s_Title">Log title (optional).</param>
		public void WriteHeader( string s_Title = "" )
		{
			if ( this.Format != LogFormat.XHTML ) {
				this.Log( "XHTML header not written, incorrect log type." );
				return;
			}

			WriteToFile( "", false );

			this.WriteToFile( "<?xml version='1.0' ?>" );
			this.WriteToFile( 
			    "<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' " +
				"'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>" );
			this.WriteToFile( "<html xmlns='http://www.w3.org/1999/xhtml'>" );
			this.WriteToFile( "<head>" );

			if ( s_Title == "" ){
				s_Title = this.File + ": " + DateTime.Now.ToString();
			}

			this.WriteToFile("<title>" + s_Title + "</title>");
			this.WriteToFile( "<style type='text/css'>body{font-family:monospace;}</style>" );
			this.WriteToFile( "</head><body>" );
		}

		/// <summary>
		/// Writes the log footer in simplified XHTML format.
		/// </summary>
		public void WriteFooter()
		{
			if ( this.Format != LogFormat.XHTML ) {
				this.Log( "XHTML footer not written, incorrect log type." );
				return;
			}

			this.WriteToFile( "</body></html>" );
		}

		/// <summary>
		/// Writes log data to local console.
		/// </summary>
		/// <param name="s_Text">String to log.</param>
		private void WriteToConsole( string s_Text )
		{
			Console.WriteLine( s_Text );
		}

		/// <summary>
		/// Writes log data to local file.
		/// </summary>
		/// <param name="s_Text">String to log.</param>
		/// <param name="b_Append">If set to <c>true</c> append to existing log file (optional).</param>
		private void WriteToFile( string s_Text, bool b_Append = true )
		{
			try {
				try {
					// Block until handle available or specific timeout reached
					hasHandle = mutex.WaitOne( 5000, false );

					if ( hasHandle == false ){
						throw new TimeoutException( "Timeout on aquiring WriteToFile handle" );
					}
				} catch ( AbandonedMutexException e ){
					// Mutex abandoned by another process, handle now available
					hasHandle = true;
					this.LogException( e );
				}

				// Open file stream and log string
				// Log IOException errors
				try {
					StreamWriter outFile = new StreamWriter( s_LogFile, b_Append, Encoding.UTF8 );
					if ( s_Text != "" ){
						outFile.WriteLine( s_Text );
					}

					outFile.Flush();
					outFile.Close();

				} catch ( IOException e ){
					this.LogException( e );
				}
			} finally {
				if ( hasHandle ) {
					mutex.ReleaseMutex(); // Release handle
				}
			}
		}

		/// <summary>
		/// Log handled exceptions.
		/// </summary>
		/// <param name="e">Exception.</param>
		/// <param name="s_Msg">String to log (optional).</param>
		private void LogException( Exception e, string s_Msg = "" )
		{
			if ( e == null ) {
				return;
			}

			string s_LogInfo;
			s_LogInfo = "EXCEPTION: " + DateTime.Now + ": ";

			// test if IOException and disable future IO logging
			// new entries are logged to the console instead
			if ( e is IOException ){
				this.Format = LogFormat.CONSOLE;

				this.WriteToConsole( e.ToString() );
				this.WriteToConsole( s_LogInfo + "IOException detected, " +
					"logging to " + this.File + " has been disabled." );
				return;
			}

			if ( this.Format != LogFormat.CONSOLE ) {
				this.WriteToFile( e.ToString() );
				this.WriteToFile( s_LogInfo + s_Msg );
				return;
			}

			this.WriteToConsole( e.ToString() );
			this.WriteToConsole( s_LogInfo + s_Msg );
		}
	}
}

