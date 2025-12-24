#pragma once
#include "IDMaker.h"
#include "RoomManager.h"
#include "MsgHandler.h"

using boost::asio::ip::tcp;

class Server
{
public:
	Server(const tcp::endpoint& endpoint, MsgHandler& msgHandler)
		: m_acceptor(ioContext, endpoint)
		, m_msgHandler(msgHandler)
	{
		instance = this;
	}

	bool Run();
protected:
	void OnAsyncAccept(boost::system::error_code ec, std::shared_ptr<tcp::socket> socket);

private:
	void DoAccept();

	std::atomic_int testID = 1;
	MsgHandler& m_msgHandler;
	static boost::asio::io_context ioContext;
	static Server* instance;

	tcp::acceptor m_acceptor;
	std::thread m_workThread;
	std::thread m_logicThread;
};
_declspec(selectany) boost::asio::io_context Server::ioContext;
_declspec(selectany) Server* Server::instance = nullptr;

