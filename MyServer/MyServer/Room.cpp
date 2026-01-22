#pragma once
#include <iostream>
#include <memory>
#include <mutex>
#include <set>
#include <map>
#include <vector>
#include <array>
#include <cstring>
#include <fstream>

#include "Room.h"
#include "RoomManager.h"
#include "Message.h"

typedef std::map<uint64_t, Room::GameState> GameStateMap;

// #region agent log
static void AppendDebugLog(const char* location, const char* message, const char* data, const char* hypothesisId)
{
	std::ofstream ofs("d:\\GitRepo\\Unity Racing Game\\.cursor\\debug.log", std::ios::app);
	if (!ofs)
		return;
	long long ts = std::chrono::duration_cast<std::chrono::milliseconds>(
		std::chrono::system_clock::now().time_since_epoch()).count();
	ofs << "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\""
		<< hypothesisId << "\",\"location\":\"" << location << "\",\"message\":\""
		<< message << "\",\"data\":" << data << ",\"timestamp\":" << ts << "}\n";
}
// #endregion

Room::Room(uint64_t roomID, uint32_t maxSessionCount)
	: m_roomID(roomID)
	, m_maxSessionCount(maxSessionCount)
{
	m_start = false;
	m_startPending = false;
	m_matchModeSet = false;
	m_minPlayers = 1;
	ContentLoader cl;
	cl.Load(m_nodeList);
	m_nodeIndex = 0;
}

void Room::Join(SessionPtr session)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	if (m_sessions.find(session->UserID()) != m_sessions.end())
		return;

	session->SetRoomInfo(m_roomID);
	m_sessions.insert({ session->UserID(), session});

	GameState gameState;
	m_gameStates.insert({ session->UserID(), gameState });
	// Join Room Log

	session->Start();
}

void Room::StartAt(steady_clock::time_point startTime)
{
	m_scheduledStart = startTime;
	m_startPending = true;
	m_progressEnd = false;
}

void Room::Leave(uint64_t userID)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	m_sessions.erase(userID);
	m_userNicknames.erase(userID);
	if(m_sessions.empty())
		RoomManager::Instance()->DeleteRoom(m_roomID);
	// Leave Room Log
	// Broadcast to Client?
}

void Room::Deliver(const Message msg)
{
	// Recv
	std::lock_guard<std::mutex> lockGuard(m_netLock);
	m_msgQueue.push(msg);
}

void Room::Broadcast()
{
	std::lock_guard<std::mutex> lockGuard(m_netLock);

	//std::cout << "[room:" << m_roomID << "] broadcasat\n";
	while (!m_msgQueue.empty())
	{
		Message& msg = m_msgQueue.front();
		m_msgQueue.pop();
		for (auto user : m_sessions)
		{
			user.second->Deliver(msg);
		}
	}
}

void Room::SendTarget(Message msg, uint64_t userID)
{
	m_sessions[userID]->Deliver(msg);
}

bool Room::CanJoin()
{
	return NumberOfPeople() < m_maxSessionCount;
}

int Room::NumberOfPeople()
{
	return m_sessions.size();
}

void Room::Update()
{
	// #region agent log
	AppendDebugLog("Room.cpp:Update", "Update entry", "{\"startPending\":true}", "A");
	// #endregion
	if (m_startPending)
	{
		steady_clock::time_point now = steady_clock::now();
		if (now >= m_scheduledStart)
		{
			m_startTime = m_preTime = m_scheduledStart;
			m_start = true;
			m_startPending = false;
		}
	}

	if (!IsStart())
	{
		return;
	}

	// 노드 생성 체크
	m_curTime = steady_clock::now();
	milliseconds progressTime = duration_cast<milliseconds>(m_curTime - m_startTime);

	if (!m_progressEnd && m_nodeIndex < m_nodeList.size() && progressTime >= m_nodeList[m_nodeIndex].time)
	{
		uint32_t nodeTimeMs = static_cast<uint32_t>(m_nodeList[m_nodeIndex].time.count());
		SpawnNode(static_cast<NoteType>(m_nodeList[m_nodeIndex].type), static_cast<NotePos>(m_nodeList[m_nodeIndex].pos), nodeTimeMs);
		++m_nodeIndex;

		if (m_nodeIndex == m_nodeList.size())
		{
			m_progressEnd = true;
		}
	}

	// 게임 종료 조건 체크: 모든 노드 스폰 완료 후 5초 경과
	if (m_progressEnd)
	{
		milliseconds timeSinceLastNode = duration_cast<milliseconds>(m_curTime - m_startTime);
		if (m_nodeList.size() > 0)
		{
			milliseconds lastNodeTime = m_nodeList[m_nodeList.size() - 1].time;
			if (timeSinceLastNode >= lastNodeTime + milliseconds(5000))
			{
				EndGame();

				Message endGamePacket;
				endGamePacket.EncodeHeader(PacketType::EndGame);
				Deliver(endGamePacket);

				std::cout << "[Room: " << m_roomID << "] Game ended - all nodes spawned and time elapsed\n";
			}
		}
	}

	MakeScoreList();
	Broadcast();
}

void Room::SetReady(uint64_t userID)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	if (m_gameStates.end() != m_gameStates.find(userID))
	{
		m_gameStates[userID].readyState = true;
	}
}

std::string Room::DecodeNickname(uint64_t part1, uint64_t part2)
{
	std::array<unsigned char, 16> bytes{};
	std::memcpy(bytes.data(), &part1, sizeof(uint64_t));
	std::memcpy(bytes.data() + 8, &part2, sizeof(uint64_t));

	size_t len = bytes.size();
	while (len > 0 && bytes[len - 1] == 0)
	{
		--len;
	}
	return std::string(reinterpret_cast<char*>(bytes.data()), len);
}

void Room::EncodeNickname(const std::string& nickname, uint64_t& part1, uint64_t& part2)
{
	std::array<unsigned char, 16> bytes{};
	if (!nickname.empty())
	{
		size_t copyLen = nickname.size() > bytes.size() ? bytes.size() : nickname.size();
		std::memcpy(bytes.data(), nickname.data(), copyLen);
	}
	std::memcpy(&part1, bytes.data(), sizeof(uint64_t));
	std::memcpy(&part2, bytes.data() + 8, sizeof(uint64_t));
}

void Room::SetNickname(uint64_t userID, uint64_t nicknamePart1, uint64_t nicknamePart2)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	std::string nickname = DecodeNickname(nicknamePart1, nicknamePart2);
	if (nickname.empty())
	{
		nickname = "Player";
	}
	m_userNicknames[userID] = nickname;
}

void Room::SetMatchMode(uint32_t matchMode)
{
	if (m_matchModeSet)
		return;

	MatchMode mode = static_cast<MatchMode>(matchMode);
	if (mode == MatchMode::Solo)
	{
		m_minPlayers = 1;
		m_maxSessionCount = 1;
	}
	else
	{
		m_minPlayers = 2;
		if (m_maxSessionCount < 2)
			m_maxSessionCount = 2;
	}

	m_matchModeSet = true;
}

bool Room::ReadyCheck()
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	bool allReady = true;
	for (auto& gameState : m_gameStates)
	{
		if (gameState.second.readyState == false)
			allReady = false;
	}

	return allReady && NumberOfPeople() >= static_cast<int>(m_minPlayers) && NumberOfPeople() <= m_maxSessionCount;
}

bool Room::IsStart()
{
	return m_start;
}

void Room::EndGame()
{
	m_start = false;
	m_startPending = false;
}

void Room::ResetGame()
{
	// #region agent log
	AppendDebugLog("Room.cpp:ResetGame", "ResetGame entry", "{\"gameStates\":0}", "B");
	// #endregion
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	
	GameStateMap::iterator iter = m_gameStates.begin();
	for (; iter != m_gameStates.end(); ++iter)
	{
		iter->second.score = 0;
		iter->second.life = 500;
		iter->second.combo = 0;
		iter->second.readyState = false;
	}
	
	// 게임 진행 상태 초기화
	m_start = false;
	m_startPending = false;
	m_nodeIndex = 0;
	m_progressEnd = false;
}

uint64_t* Room::GetUserList()
{
	// #region agent log
	AppendDebugLog("Room.cpp:GetUserList", "GetUserList entry", "{\"userCount\":0}", "C");
	// #endregion
	int arraySize = NumberOfPeople();
	uint64_t userList[MAX_SESSION_SIZE];
	int userIndex = 0;
	GameStateMap::iterator iter = m_gameStates.begin();
	for (; iter != m_gameStates.end(); ++iter)
	{
		userList[userIndex] = iter->first;
		++userIndex;
	}

	return userList;
}

void Room::SpawnNode(NoteType type, NotePos pos, uint32_t nodeTimeMs)
{
	Message msg;
	msg.PutData(reinterpret_cast<char*>(&type), sizeof(NoteType));
	msg.PutData(reinterpret_cast<char*>(&pos), sizeof(NotePos));
	msg.PutData(reinterpret_cast<char*>(&nodeTimeMs), sizeof(uint32_t));
	msg.EncodeHeader(PacketType::SpawnNode);

	Deliver(msg);
	std::cout << "[room: " << m_roomID << "] SpawnNode Type: " << static_cast<int>(type) << " Pos: " << static_cast<int>(pos) << " TimeMs: " << nodeTimeMs << " Index: "  << m_nodeIndex << "\n";
}

void Room::CalculateScore(uint64_t userID, uint32_t nodeType, uint32_t judgmentType, float timeDifference, int32_t judgmentScore)
{
	NoteType type = static_cast<NoteType>(nodeType);
	uint64_t score = 0;
	uint64_t penalty = 0;
	int life = 0;

	// 판정 타입에 따른 점수 배율 (Perfect: 1.0, Good: 0.7, Bad: 0.3, Miss: 0.0)
	double judgmentMultiplier = 1.0;
	switch (judgmentType)
	{
	case 0: // Perfect
		judgmentMultiplier = 1.0;
		break;
	case 1: // Good
		judgmentMultiplier = 0.7;
		break;
	case 2: // Bad
		judgmentMultiplier = 0.3;
		break;
	case 3: // Miss
		judgmentMultiplier = 0.0;
		break;
	default:
		judgmentMultiplier = 1.0; // 기본값 (판정 정보가 없는 경우)
		break;
	}

	switch (type)
	{
	case NoteType::ObjectA:
		score = static_cast<uint64_t>(3000 * judgmentMultiplier);
		break;
	case NoteType::ObjectB:
		score = static_cast<uint64_t>(2000 * judgmentMultiplier);
		break;
	case NoteType::ObjectC:
		score = static_cast<uint64_t>(4500 * judgmentMultiplier);
		break;
	case NoteType::AFail:
		penalty = 3000;
		life = -100;
		break;
	case NoteType::BFail:
		penalty = 2000;
		life = -100;
		break;
	case NoteType::CFail:
		penalty = 4500;
		life = -300;
		break;
	}

	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);

	if (type < NoteType::AFail)
	{
		// Perfect와 Good만 콤보 유지, Bad와 Miss는 콤보 초기화  // Perfect(0) or Good(1)
		if(judgmentType <= 1)
			++m_gameStates[userID].combo;
		else // Bad(2) or Miss(3)
		{
			m_gameStates[userID].combo = 0;
		}
	}
	else
	{
		m_gameStates[userID].combo = 0;
	}

	m_gameStates[userID].life = (m_gameStates[userID].life - life > 0) ? m_gameStates[userID].life - life : 0;
	int combo = m_gameStates[userID].combo;
	if (score > 0)
	{
		m_gameStates[userID].score += static_cast<uint64_t>(score * (1.1 + (combo / static_cast<double>(1000))));
	}
	else if (penalty > 0)
	{
		if (m_gameStates[userID].score >= penalty)
			m_gameStates[userID].score -= penalty;
		else
			m_gameStates[userID].score = 0;
	}
}

void Room::MakeScoreList()
{
	// #region agent log
	AppendDebugLog("Room.cpp:MakeScoreList", "MakeScoreList entry", "{\"scoreCount\":0}", "D");
	// #endregion
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);

	Message msg;
	uint32_t sessionCount = NumberOfPeople();
	msg.PutData(reinterpret_cast<char*>(&sessionCount), sizeof(uint32_t));
	GameStateMap::iterator iter = m_gameStates.begin();
	for (; iter != m_gameStates.end(); ++iter)
	{
		uint64_t userID = iter->first;
		uint64_t score = iter->second.score;
		uint64_t nicknamePart1 = 0;
		uint64_t nicknamePart2 = 0;
		auto nicknameIt = m_userNicknames.find(userID);
		if (nicknameIt != m_userNicknames.end())
		{
			EncodeNickname(nicknameIt->second, nicknamePart1, nicknamePart2);
		}
		msg.PutData(reinterpret_cast<char*>(&userID), sizeof(userID));
		msg.PutData(reinterpret_cast<char*>(&score), sizeof(score));
		msg.PutData(reinterpret_cast<char*>(&nicknamePart1), sizeof(nicknamePart1));
		msg.PutData(reinterpret_cast<char*>(&nicknamePart2), sizeof(nicknamePart2));
	}
	msg.EncodeHeader(PacketType::ScoreBroadcast);

	milliseconds interval = duration_cast<milliseconds>(m_curTime - m_preTime);
	m_preTime = high_resolution_clock::now();
	std::cout << "[room: " << m_roomID << "] Score BroadCast [interval: " << interval.count() <<  "]\n";

	Deliver(msg);
}

