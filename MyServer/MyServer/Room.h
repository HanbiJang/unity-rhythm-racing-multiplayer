#pragma once
#include <chrono>
#include <map>
#include <string>

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
		MAX_SESSION_SIZE = 2,  // 최대 인원수 (1인 플레이도 가능)
		MIN_SESSION_SIZE = 1,  // 최소 인원수 (1인 플레이 가능)
	};

public:
	enum class MatchMode : uint32_t
	{
		Solo = 0,
		Multi = 1,
	};
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
	void StartAt(steady_clock::time_point startTime);
	void Leave(uint64_t userID);
	void Deliver(const Message msg);
	void Broadcast();
	void SendTarget(const Message msg, uint64_t userID);
	
	bool CanJoin();
	int NumberOfPeople();

	void Update();

	// contents
	void SetReady(uint64_t userID);
	void SetMatchMode(uint32_t matchMode);
	void SetNickname(uint64_t userID, uint64_t nicknamePart1, uint64_t nicknamePart2);
	bool ReadyCheck();
	bool IsStart();
	void EndGame();
	void ResetGame();
	uint64_t* GetUserList();

	void SpawnNode(NoteType type,  NotePos pos, uint32_t nodeTimeMs = 0);

	void CalculateScore(uint64_t userID, uint32_t nodeType, uint32_t judgmentType = 0, float timeDifference = 0.0f, int32_t judgmentScore = 0);
	void MakeScoreList();
	
	int GetTotalNoteCount() const { return static_cast<int>(m_nodeList.size()); }

private:
	static std::string DecodeNickname(uint64_t part1, uint64_t part2);
	static void EncodeNickname(const std::string& nickname, uint64_t& part1, uint64_t& part2);

	uint64_t m_roomID;
	uint32_t m_maxSessionCount;
	std::map<uint64_t, SessionPtr> m_sessions;
	std::map<uint64_t, GameState> m_gameStates;
	std::map<uint64_t, std::string> m_userNicknames;
	MsgQueue m_msgQueue;

	bool m_start;
	bool m_startPending;
	steady_clock::time_point m_scheduledStart;
	bool m_matchModeSet;
	uint32_t m_minPlayers;

	steady_clock::time_point m_startTime;
	steady_clock::time_point m_curTime;
	steady_clock::time_point m_preTime;

	std::vector<Node> m_nodeList;
	int m_nodeIndex;
	bool m_progressEnd;

	std::mutex m_netLock;
	std::mutex m_gameStateLock;
};
using RoomPtr = std::shared_ptr<Room>;