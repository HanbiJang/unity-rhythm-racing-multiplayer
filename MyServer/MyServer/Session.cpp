#pragma once
#include <iostream>
#include <memory>

#include "MsgHandler.h"
#include "Session.h"
#include "RoomManager.h"

uint64_t Session::UserID()
{
	return m_userID;
}

void Session::Start()
{
	DoRead();
}

void Session::Deliver(Message& msg)
{
	m_writeBuffer.Write(reinterpret_cast<char*>(&msg.Header()), sizeof(Message::PacketHeader));
	m_writeBuffer.Write(msg.Data(), msg.Header().bodyLength);
	DoWrite();
}

void Session::SetRoomInfo(uint64_t roomID)
{
	m_roomID = roomID;
}

void Session::OnAsyncRead(boost::system::error_code ec, std::size_t length)
{
	m_readBuffer.SetEndIndex(length);
	if (!ec)
	{
		while (m_readBuffer.UsingSize() > sizeof(Message::PacketHeader))
		{
			if (m_readBuffer.Read(reinterpret_cast<char*>(&m_readMsg.Header()), sizeof(Message::PacketHeader)) == false)
				break;

			if (m_readBuffer.Read(m_readMsg.Data(), m_readMsg.Header().bodyLength) == false)
				break;

			// value setting check -> i thk it is unuseful
			m_readMsg.SetBodyLength(m_readMsg.Header().bodyLength);

			MsgHandler::Instance()->Push(m_readMsg);
		}

		m_readBuffer.Clear();
		DoRead();
	}
	else
	{
		std::cout << "Read Fail [Error] " << ec.message() << "\n";
		RoomPtr room = RoomManager::Instance()->FindRoom(m_roomID);
		room->Leave(m_userID);

		m_socket->shutdown(tcp::socket::shutdown_both, ec);
		m_socket->close();
	}
}

void Session::DoRead()
{
	boost::asio::async_read(*m_socket, boost::asio::buffer(m_readBuffer.Data(), NetBuffer::Type::Recv)
		, boost::asio::transfer_at_least(sizeof(Message::PacketHeader))
		, std::bind(&Session::OnAsyncRead, shared_from_this(), std::placeholders::_1, std::placeholders::_2));
}

void Session::OnAsyncWrite(boost::system::error_code ec, std::size_t length)
{
	if (!ec)
	{
		m_writeBuffer.Clear();
	}
	else
	{
		std::cout << "Write Fail [Error]" << ec << "\n";
		RoomPtr room = RoomManager::Instance()->FindRoom(m_roomID);
		room->Leave(m_userID);

		m_socket->shutdown(tcp::socket::shutdown_both, ec);
		m_socket->close();
	}
}

void Session::DoWrite()
{
	boost::asio::async_write(*m_socket, boost::asio::buffer(m_writeBuffer.Data(), m_writeBuffer.UsingSize()),
		std::bind(&Session::OnAsyncWrite, shared_from_this(), std::placeholders::_1, std::placeholders::_2));
}
