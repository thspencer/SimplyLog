SimplyLog
=========

A simple logger library written in C# that I wrote for a previous project.

License
=======

SimplyLog is licensed under the [GPL v3 license](http://www.tldrlegal.com/license/gnu-general-public-license-v3-%28gpl-3%29)

Usage
=====

      // Initialize logger and set parameters
  		Logger SimplyLog.Logging = new SimplyLog.Logging();
			Logger.File		= "simplylog.log";
			Logger.Format	= SimplyLog.Logging.LogFormat.XHTML;
			Logger.Level 	= SimplyLog.Logging.LogLevel.EXCEPTION |
                      SimplyLog.Logging.LogLevel.ERROR     |
                      SimplyLog.Logging.LogLevel.DEBUG;
                      
      // Write a XHTML compliant file header
      Logger.WriteHeader();
      
      // Various log message styles
      Logger.Log( "Just a string" );
      Logger.Log( "A string with a loglevel", SimplyLog.Logging.LogLevel.DEBUG );
      Logger.Log( "A string with an Exception, SimplyLog.Logging.LogLevel.EXCEPTION, someException );
      
      // Write a XHTML compliant file footer
      Logger.WriteFooter();
