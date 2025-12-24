#include "Logger.h"

Logger::Logger()
{
	SetGlobalAttribute();

	AddConsoleSink();
	AddFileSink();
}

Logger::~Logger()
{
	_fileSink->stop();
	_fileSink->flush();
	_fileSink.reset();

	_consoleSink->flush();
	_consoleSink.reset();

	logging::core::get()->remove_all_sinks();
}


void Logger::SetGlobalAttribute()
{
	logging::core::get()->add_global_attribute("TimeStamp", attrs::local_clock());
	logging::core::get()->add_global_attribute("RecordID", attrs::counter< unsigned int >());
}

void Logger::AddConsoleSink()
{
	_consoleSink = boost::make_shared<ConsoleSink>(boost::make_shared<console_sink>(),
		keywords::order = logging::make_attr_ordering("RecordID", std::less<unsigned int>()));

	_consoleSink->locked_backend()->add_stream(boost::shared_ptr<std::ostream>(&std::clog, boost::null_deleter()));
	_consoleSink->locked_backend()->auto_flush(true);

	_consoleSink->set_formatter(
		expr::stream
		<< "<" << expr::attr<boost::thread::id>("ThreadID") << ">"
		<< "[" << expr::attr<unsigned int>("RecordID") << "]"
		<< "["
		<< expr::format_date_time< boost::posix_time::ptime >("TimeStamp", "%Y-%m-%d %H:%M:%S")
		<< "]"
		<< expr::smessage
	);

	logging::core::get()->add_sink(_consoleSink);
}

void Logger::AddFileSink()
{
	_fileSink = boost::make_shared<FileSink>(boost::make_shared<file_sink>(),
		keywords::order = logging::make_attr_ordering("RecordID", std::less<unsigned int>()));

	_fileSink->locked_backend()->set_file_collector(sinks::file::make_collector(keywords::target = "logs"));
	_fileSink->locked_backend()->set_rotation_size(1024 * 10 * 10);
	_fileSink->locked_backend()->set_time_based_rotation(sinks::file::rotation_at_time_point(0, 0, 0));
	_fileSink->locked_backend()->set_open_mode(std::ios_base::out | std::ios_base::app);
	_fileSink->locked_backend()->set_file_name_pattern("log_%Y%m%d_%N.log");
	_fileSink->locked_backend()->scan_for_files();

	_fileSink->set_formatter(
		expr::stream
		<< "<" << expr::attr<boost::thread::id>("ThreadID") << ">"
		<< "[" << expr::attr<unsigned int>("RecordID") << "]"
		<< "["
		<< expr::format_date_time< boost::posix_time::ptime >("TimeStamp", "%Y-%m-%d %H:%M:%S")
		<< "]"
		<< expr::smessage
	);

	logging::core::get()->add_sink(_fileSink);
}