#pragma once
#include <chrono>
#include <map>

#include "ContentLoader.h"
#include "Session.h"

using namespace std::chrono;

enum class NoteType : uint32_t
{
	ObjectA = 0,
	ObjectB,
	ObjectC,
	AFail,
	BFail,
	CFail,
	Max
};

enum class NotePos : int
{
	Left,
	Center,
	Right,
};

class Room
{
	enum
	{
		MAX_SESSION_SIZE = 2,
	};

public:
	struct GameState
	{
		uint64_t score;
		int life;
		int combo;
		bool readyState;

		GameState()
		{
			score = 0;
			life = 500;
			combo = 0;
			readyState = false;
		}
	};

	Room(uint64_t roomID, uint32_t maxSessionCount = MAX_SESSION_SIZE);

	void Join(SessionPtr session);
	void Start();
	void Leave(uint64_t userID);
	void Deliver(const Message msg);
	void Broadcast();
	void SendTarget(const Message msg, uint64_t userID);
	
	bool CanJoin();
	int NumberOfPeople();

	void Update();

	// contents
	void SetReady(uint64_t userID);
	bool ReadyCheck();
	bool IsStart();
	void EndGame();
	void ResetGame();
	uint64_t* GetUserList();

	void SpawnNode(NoteType type,  NotePos pos);

	void CalculateScore(uint64_t userID, uint32_t nodeType);
	void MakeScoreList();

private:
	uint64_t m_roomID;
	uint32_t m_maxSessionCount;
	std::map<uint64_t, SessionPtr> m_sessions;
	std::map<uint64_t, GameState> m_gameStates;
	MsgQueue m_msgQueue;

	bool m_start;

	high_resolution_clock::time_point m_startTime;
	high_resolution_clock::time_point m_curTime;
	high_resolution_clock::time_point m_preTime;

	std::vector<Node> m_nodeList;
	int m_nodeIndex;
	bool m_progressEnd;

	std::mutex m_netLock;
	std::mutex m_gameStateLock;
};
using RoomPtr = std::shared_ptr<Room>;