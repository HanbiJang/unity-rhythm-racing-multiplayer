#pragma once
#include "FrameTimer.h"
#include "IDMaker.h"
#include "Singleton.h"
#include "Room.h"

class RoomManager : public Singleton<RoomManager>
{
public:
	RoomManager();
	virtual ~RoomManager() {};

	void MatchMaking(SessionPtr session);
	RoomPtr FindRoom(uint64_t roomID);
	void DeleteRoom(uint64_t roomID);

	void DoLogic();

private:
	uint64_t GetJoinableRoom();
	uint64_t NextRoomID();

private:
	std::atomic_int testID = 1;
	std::map<uint64_t, RoomPtr> m_rooms;
	std::mutex m_roomLock;

	FrameTimer m_frameTimer;
};

