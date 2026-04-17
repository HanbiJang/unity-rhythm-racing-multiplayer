#pragma once
#include <queue>
#include <vector>
#include <boost/asio.hpp>
#include "NetBuffer.h"
#include "Message.h"

using boost::asio::ip::tcp;

class Session : public std::enable_shared_from_this<Session>
{
public:
	Session() = delete;
	Session(std::shared_ptr<tcp::socket> socket, uint64_t userID)
		: m_socket(std::move(socket))
		, m_roomID(0)
		, m_userID(userID)
		, m_isWriting(false)
	{}

	uint64_t UserID();
	void Start();
	void Deliver(Message& msg);
	void SetRoomInfo(uint64_t roomID);

protected:
	void OnAsyncRead(boost::system::error_code ec, std::size_t length);
	void DoRead();

	void OnAsyncWrite(boost::system::error_code ec, std::size_t length);
	void DoWrite();

private:
	std::shared_ptr<tcp::socket> m_socket;
	NetBuffer m_readBuffer;
	Message m_readMsg;

	std::queue<std::vector<char>> m_sendQueue;
	bool m_isWriting;

	uint64_t m_userID;
	uint64_t m_roomID;
};
using SessionPtr = std::shared_ptr<Session>;
