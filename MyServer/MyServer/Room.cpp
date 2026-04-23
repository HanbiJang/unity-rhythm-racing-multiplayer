#define NOMINMAX
#include <iostream>
#include <memory>
#include <mutex>
#include <set>
#include <map>
#include <vector>
#include <array>
#include <cstring>
#include <fstream>
#include <chrono>

#include "Room.h"
#include "RoomManager.h"
#include "Message.h"

using namespace std::chrono;

typedef std::map<uint64_t, Room::GameState> GameStateMap;

// #region agent log
static void AppendDebugLog(const char* location, const char* message, const char* data, const char* hypothesisId)
{
	std::ofstream ofs("d:\\GitRepo\\Unity Racing Game\\.cursor\\debug.log", std::ios::app);
	if (!ofs) return;
	long long ts = duration_cast<milliseconds>(system_clock::now().time_since_epoch()).count();
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
	m_roomSpeedLevel = 0;
	m_nextExtraNodeTime = milliseconds(-1);
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
	m_sessions.insert({ session->UserID(), session });

	GameState gameState;
	m_gameStates.insert({ session->UserID(), gameState });

	session->Start();
}

void Room::StartAt(steady_clock::time_point startTime)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	m_scheduledStart = startTime;
	m_startPending = true;
	m_progressEnd = false;
}

void Room::Leave(uint64_t userID)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	m_sessions.erase(userID);
	m_userNicknames.erase(userID);
	if (m_sessions.empty())
		RoomManager::Instance()->DeleteRoom(m_roomID);
}

void Room::Deliver(const Message msg)
{
	std::lock_guard<std::mutex> lockGuard(m_netLock);
	m_msgQueue.push(msg);
}

void Room::Broadcast()
{
	std::lock_guard<std::mutex> lockGuard(m_netLock);
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
	// 필요 시 m_sessions 접근에 대한 안전 장치 추가 고려
	m_sessions[userID]->Deliver(msg);
}

bool Room::CanJoin()
{
	return NumberOfPeople() < m_maxSessionCount;
}

int Room::NumberOfPeople()
{
	return static_cast<int>(m_sessions.size());
}

void Room::Update()
{
	// 게임 상태 관련 로직은 Lock 내에서 처리
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);

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

	m_curTime = steady_clock::now();
	milliseconds progressTime = duration_cast<milliseconds>(m_curTime - m_startTime);

	// 1. 노드 생성 체크
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

	// 3. 게임 종료 조건 체크
	if (m_progressEnd)
	{
		milliseconds timeSinceLastNode = duration_cast<milliseconds>(m_curTime - m_startTime);
		if (!m_nodeList.empty())
		{
			milliseconds lastNodeTime = m_nodeList.back().time;
			if (timeSinceLastNode >= lastNodeTime + milliseconds(5000))
			{
				EndGame();
				Message endGamePacket;
				endGamePacket.EncodeHeader(PacketType::EndGame);
				Deliver(endGamePacket);
			}
		}
	}

	MakeScoreList();
	Broadcast();
}

void Room::SetReady(uint64_t userID)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	if (m_gameStates.count(userID))
	{
		m_gameStates[userID].readyState = true;
	}
}

// ... (DecodeNickname, EncodeNickname 함수는 기존과 동일)
std::string Room::DecodeNickname(uint64_t part1, uint64_t part2)
{
	std::array<unsigned char, 16> bytes{};
	std::memcpy(bytes.data(), &part1, sizeof(uint64_t));
	std::memcpy(bytes.data() + 8, &part2, sizeof(uint64_t));
	size_t len = bytes.size();
	while (len > 0 && bytes[len - 1] == 0) --len;
	return std::string(reinterpret_cast<char*>(bytes.data()), len);
}

void Room::EncodeNickname(const std::string& nickname, uint64_t& part1, uint64_t& part2)
{
	std::array<unsigned char, 16> bytes{};
	if (!nickname.empty()) {
		size_t copyLen = std::min(nickname.size(), bytes.size());
		std::memcpy(bytes.data(), nickname.data(), copyLen);
	}
	std::memcpy(&part1, bytes.data(), sizeof(uint64_t));
	std::memcpy(&part2, bytes.data() + 8, sizeof(uint64_t));
}

void Room::SetNickname(uint64_t userID, uint64_t nicknamePart1, uint64_t nicknamePart2)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	std::string nickname = DecodeNickname(nicknamePart1, nicknamePart2);
	m_userNicknames[userID] = nickname.empty() ? "Player" : nickname;
}

void Room::SetMatchMode(uint32_t matchMode)
{
	if (m_matchModeSet) return;
	MatchMode mode = static_cast<MatchMode>(matchMode);
	m_minPlayers = (mode == MatchMode::Solo) ? 1 : 2;
	if (mode != MatchMode::Solo && m_maxSessionCount < 2) m_maxSessionCount = 2;
	m_matchModeSet = true;
}

bool Room::ReadyCheck()
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	for (auto& pair : m_gameStates)
	{
		if (!pair.second.readyState) return false;
	}
	int count = NumberOfPeople();
	return count >= static_cast<int>(m_minPlayers) && count <= static_cast<int>(m_maxSessionCount);
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
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	for (auto& pair : m_gameStates)
	{
		pair.second.score = 0;
		pair.second.life = 500;
		pair.second.combo = 0;
		pair.second.readyState = false;
	}
	m_start = false;
	m_startPending = false;
	m_nodeIndex = 0;
	m_progressEnd = false;
	m_roomSpeedLevel = 0;
	m_nextExtraNodeTime = milliseconds(-1);
}

// GetUserList는 필요에 따라 구현 (기존의 불안전한 포인터 반환보다는 vector<uint64_t> 반환 권장)
// 여기서는 기존 로직 유지
uint64_t* Room::GetUserList()
{
	static uint64_t userList[100]; // MAX_SESSION_SIZE 가정
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	int i = 0;
	for (auto& pair : m_gameStates)
	{
		userList[i++] = pair.first;
	}
	return userList;
}

void Room::SetSpeedLevel(uint64_t userID, int32_t speedLevel)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	if (m_gameStates.find(userID) == m_gameStates.end()) return;

	m_roomSpeedLevel = (std::max)(0, speedLevel);
	if (m_roomSpeedLevel == 0)
		m_nextExtraNodeTime = milliseconds(-1);
}

void Room::SpawnNode(NoteType type, NotePos pos, uint32_t nodeTimeMs)
{
	Message msg;
	msg.PutData(reinterpret_cast<char*>(&type), sizeof(NoteType));
	msg.PutData(reinterpret_cast<char*>(&pos), sizeof(NotePos));
	msg.PutData(reinterpret_cast<char*>(&nodeTimeMs), sizeof(uint32_t));
	msg.EncodeHeader(PacketType::SpawnNode);
	Deliver(msg);
}

void Room::CalculateScore(uint64_t userID, uint32_t nodeType, uint32_t judgmentType, float timeDifference, int32_t judgmentScore)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	if (m_gameStates.find(userID) == m_gameStates.end()) return;

	NoteType type = static_cast<NoteType>(nodeType);
	double multiplier = (judgmentType == 0) ? 1.0 : (judgmentType == 1 ? 0.7 : (judgmentType == 2 ? 0.3 : 0.0));

	uint64_t score = 0, penalty = 0;
	int lifeDelta = 0;

	if (type < NoteType::AFail)
	{
		score = static_cast<uint64_t>((type == NoteType::ObjectA ? 3000 : (type == NoteType::ObjectB ? 2000 : 4500)) * multiplier);
		m_gameStates[userID].combo = (judgmentType <= 1) ? m_gameStates[userID].combo + 1 : 0;
	}
	else
	{
		penalty = (type == NoteType::CFail) ? 4500 : (type == NoteType::AFail ? 3000 : 2000);
		lifeDelta = (type == NoteType::CFail) ? -300 : -100;
		m_gameStates[userID].combo = 0;
	}

	GameState& gs = m_gameStates[userID];
	gs.life = (std::max)(0, gs.life - lifeDelta);
	if (score > 0)
		gs.score += static_cast<uint64_t>(score * (1.1 + (gs.combo / 1000.0)));
	else if (penalty > 0)
		gs.score = (gs.score >= penalty) ? gs.score - penalty : 0;
}

void Room::MakeScoreList()
{
	Message msg;
	uint32_t sessionCount = NumberOfPeople();
	msg.PutData(reinterpret_cast<char*>(&sessionCount), sizeof(uint32_t));
	for (auto& pair : m_gameStates)
	{
		uint64_t userID = pair.first;
		uint64_t score = pair.second.score;
		uint64_t p1 = 0, p2 = 0;
		if (m_userNicknames.count(userID))
			EncodeNickname(m_userNicknames[userID], p1, p2);
		msg.PutData(reinterpret_cast<char*>(&userID), sizeof(userID));
		msg.PutData(reinterpret_cast<char*>(&score), sizeof(score));
		msg.PutData(reinterpret_cast<char*>(&p1), sizeof(p1));
		msg.PutData(reinterpret_cast<char*>(&p2), sizeof(p2));
	}
	msg.EncodeHeader(PacketType::ScoreBroadcast);
	Deliver(msg);
	m_preTime = m_curTime;
}