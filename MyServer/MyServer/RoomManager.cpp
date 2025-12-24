#pragma once
#include <memory>
#include <map>
#include <iostream>

#include "Session.h"
#include "MsgHandler.h"
#include "RoomManager.h"

RoomManager::RoomManager()
{
}

void RoomManager::MatchMaking(SessionPtr session)
{
	uint64_t roomID = GetJoinableRoom();

	if (roomID == 0)
	{
		std::lock_guard<std::mutex> lockGuard(m_roomLock);
		roomID = NextRoomID();
		RoomPtr room = std::make_shared<Room>(roomID);
		m_rooms.insert({ roomID, room });
	}

	m_rooms[roomID]->Join(session);
	std::cout << "Accept UserID : " << session->UserID() <<"RoomID : " << roomID <<"\n";
	Message msg = MsgHandler::Instance()->MakePacket(PacketType::JoinGame, roomID, session->UserID());
	MsgHandler::Instance()->Push(msg);
}

RoomPtr RoomManager::FindRoom(uint64_t roomID)
{
	if (m_rooms.find(roomID) == m_rooms.end())
		return nullptr;

	return m_rooms[roomID];
}

void RoomManager::DeleteRoom(uint64_t roomID)
{
	std::lock_guard<std::mutex> lockGuard(m_roomLock);
	m_rooms.erase(roomID);
}

void RoomManager::DoLogic()
{
	while (true)
	{
		if (m_frameTimer.DoFrameSkip())
		{
			//std::cout << "FrameSkip\n";
			Sleep(1);
			continue;
		}

		for (auto& room : m_rooms)
		{
			room.second->Update();
		}
	}
}

uint64_t RoomManager::GetJoinableRoom()
{
	if (m_rooms.empty())
		return 0;

	std::lock_guard<std::mutex> lockGuard(m_roomLock);
	for (auto p : m_rooms)
	{
		RoomPtr room = p.second;
		if (room->CanJoin())
			return p.first;
	}

	return 0;
}

uint64_t RoomManager::NextRoomID()
{
	return testID.fetch_add(1);
	//return IDMaker::Instance()->GetUniqueID();
}
