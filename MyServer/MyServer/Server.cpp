#pragma once
#include <atomic>
#include <iostream>
#include <memory>
#include <boost/asio.hpp>
#include <boost/thread.hpp>

#include "Server.h"
#include "RoomManager.h"

bool Server::Run()
{
    std::cout << "Server is run\n";
    DoAccept();
    m_workThread = std::thread(std::bind(&MsgHandler::DoWork, &m_msgHandler));
    
    RoomManager* roomManagerInstance = RoomManager::Instance();
    m_logicThread = std::thread(std::bind(&RoomManager::DoLogic, roomManagerInstance));
    ioContext.run();
    m_workThread.join();
    m_logicThread.join();
    
    return false;
}

void Server::OnAsyncAccept(boost::system::error_code ec, std::shared_ptr<tcp::socket> socket)
{
    if (!ec)
    {
        //uint64_t userID = IDMaker::Instance()->GetUniqueID();
        uint64_t userID = testID.fetch_add(1);
        SessionPtr session = std::make_shared<Session>(socket, userID);
        
        RoomManager::Instance()->MatchMaking(session);
    }
    DoAccept();
}

void Server::DoAccept()
{
    auto socket = std::make_shared<tcp::socket>(ioContext);
    m_acceptor.async_accept(*socket, std::bind(&Server::OnAsyncAccept, this, std::placeholders::_1, socket));
}
