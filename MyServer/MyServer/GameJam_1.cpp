#pragma once
#include <cstdlib>
#include <iostream>
#include <boost/asio.hpp>

#include "Server.h"

using boost::asio::ip::tcp;

int main()
{
	IDMaker::Create();
	RoomManager::Create();

	MsgHandler msgHandler;

	tcp::endpoint endpoint(tcp::v4(), 8888);
	Server server(endpoint, msgHandler);

	server.Run();

	return 0;
}