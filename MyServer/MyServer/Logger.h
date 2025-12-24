#pragma once
#include <boost/core/null_deleter.hpp>
#include <boost/ref.hpp>
#include <boost/bind.hpp>
#include <boost/smart_ptr/shared_ptr.hpp>
#include <boost/date_time/posix_time/posix_time.hpp>
#include <boost/log/expressions/formatters/date_time.hpp>
#include <boost/log/support/date_time.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/barrier.hpp>

#include <boost/log/core.hpp>
#include <boost/log/trivial.hpp>
#include <boost/log/common.hpp>
#include <boost/log/expressions.hpp>
#include <boost/log/attributes.hpp>
#include <boost/log/sinks.hpp>
#include <boost/log/sources/logger.hpp>
#include <boost/log/utility/record_ordering.hpp>
#include <boost/log/utility/manipulators/add_value.hpp>
#include <boost/log/utility/setup/file.hpp>

#include <chrono>
#include <ctime>
#include <iomanip>
#include <string>
#include<sstream>
#include <string>
#include <fstream>

namespace attrs = boost::log::attributes;
namespace logging = boost::log;
namespace src = boost::log::sources;
namespace sinks = boost::log::sinks;
namespace keywords = boost::log::keywords;
namespace expr = boost::log::expressions;
using namespace logging::trivial;

//라인 넘버링과 해당 소스파일 출력을 첨가하기 위한 매크로
#define CUSTOM_LOG_TRACE(sev) \
BOOST_LOG_SCOPED_THREAD_TAG("ThreadID", boost::this_thread::get_id());\
BOOST_LOG_SEV(global_lg::get(), sev) <<"["<< sev <<"]" << ":"

//글로벌 로거 생성을 위한 매크로 - thread-safe를 위해 글로벌 로거 사용
BOOST_LOG_INLINE_GLOBAL_LOGGER_DEFAULT(global_lg, src::logger_mt)

class Logger {
public:
	typedef sinks::text_ostream_backend console_sink;
	typedef sinks::text_file_backend file_sink;

	typedef sinks::asynchronous_sink<sinks::text_file_backend, sinks::unbounded_ordering_queue
		<logging::attribute_value_ordering<unsigned int, std::less<unsigned int>>>> FileSink;
	typedef sinks::synchronous_sink<console_sink> ConsoleSink;

	Logger();
	~Logger();

private:
	void SetGlobalAttribute();

	void AddConsoleSink();
	void AddFileSink();

private:
	boost::shared_ptr<ConsoleSink> _consoleSink;
	boost::shared_ptr<FileSink> _fileSink;
};