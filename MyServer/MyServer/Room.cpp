#pragma once
#include <iostream>
#include <memory>
#include <mutex>
#include <set>
#include <vector>

#include "Room.h"
#include "RoomManager.h"
#include "Message.h"

Room::Room(uint64_t roomID, uint32_t maxSessionCount)
	: m_roomID(roomID)
	, m_maxSessionCount(maxSessionCount)
{
	m_start = false;
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

void Room::Start()
{
	m_startTime = m_preTime = high_resolution_clock::now();
	m_start = true;
	m_progressEnd = false;
}

void Room::Leave(uint64_t userID)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	m_sessions.erase(userID);
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
	if (IsStart())
	{
		// 노드 생성 체크
		m_curTime = high_resolution_clock::now();
		milliseconds progressTime = duration_cast<milliseconds>(m_curTime - m_startTime);
		
		if (m_progressEnd != true && m_nodeIndex < m_nodeList.size() && progressTime >= m_nodeList[m_nodeIndex].time)
		{
			SpawnNode(static_cast<NoteType>(m_nodeList[m_nodeIndex].type), static_cast<NotePos>(m_nodeList[m_nodeIndex].pos));
			++m_nodeIndex;

			if(m_nodeIndex == m_nodeList.size())
				m_progressEnd = true;
		}
		
		// 게임 종료 조건 체크: 모든 노드 스폰 완료 후 5초 경과
		if (m_progressEnd && !m_start)
		{
			// 이미 종료 처리됨
		}
		else if (m_progressEnd)
		{
			// 마지막 노드 스폰 시간 확인
			milliseconds timeSinceLastNode = duration_cast<milliseconds>(m_curTime - m_startTime);
			if (m_nodeList.size() > 0)
			{
				milliseconds lastNodeTime = m_nodeList[m_nodeList.size() - 1].time;
				// 마지막 노드 스폰 후 5초 경과 시 게임 종료
				if (timeSinceLastNode >= lastNodeTime + milliseconds(5000))
				{
					EndGame();
					
					// 모든 클라이언트에 EndGame 패킷 브로드캐스트
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
}

void Room::SetReady(uint64_t userID)
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	if (m_gameStates.end() != m_gameStates.find(userID))
	{
		m_gameStates[userID].readyState = true;
	}
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

	// 1인 플레이도 가능하도록 수정: 최소 1명 이상이고 모두 Ready 상태면 시작 가능
	return allReady && NumberOfPeople() >= 1 && NumberOfPeople() <= m_maxSessionCount;
}

bool Room::IsStart()
{
	return m_start;
}

void Room::EndGame()
{
	m_start = false;
}

void Room::ResetGame()
{
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);
	
	// 게임 상태 초기화
	for (auto& gameState : m_gameStates)
	{
		gameState.second.score = 0;
		gameState.second.life = 500;
		gameState.second.combo = 0;
		gameState.second.readyState = false;
	}
	
	// 게임 진행 상태 초기화
	m_start = false;
	m_nodeIndex = 0;
	m_progressEnd = false;
}

uint64_t* Room::GetUserList()
{
	int arraySize = NumberOfPeople();
	uint64_t userList[MAX_SESSION_SIZE];
	int userIndex = 0;
	for (auto& gameState : m_gameStates)
	{
		userList[userIndex] = gameState.first;
		++userIndex;
	}

	return userList;
}

void Room::SpawnNode(NoteType type, NotePos pos)
{
	Message msg;
	msg.PutData(reinterpret_cast<char*>(&type), sizeof(NoteType));
	msg.PutData(reinterpret_cast<char*>(&pos), sizeof(NotePos));
	msg.EncodeHeader(PacketType::SpawnNode);

	Deliver(msg);
	std::cout << "[room: " << m_roomID << "] SpawnNode Type: " << static_cast<int>(type) << " Pos: " << static_cast<int>(pos) << " Index: "  << m_nodeIndex << "\n";
}

void Room::CalculateScore(uint64_t userID, uint32_t nodeType)
{
	NoteType type = static_cast<NoteType>(nodeType);
	uint64_t score = 0;
	uint64_t penalty = 0;
	int life = 0;

	switch (type)
	{
	case NoteType::ObjectA:
		score = 3000;
		break;
	case NoteType::ObjectB:
		score = 2000;
		break;
	case NoteType::ObjectC:
		score = 4500;
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
		++m_gameStates[userID].combo;
	}
	else
	{
		m_gameStates[userID].combo = 0;
	}

	m_gameStates[userID].life = (m_gameStates[userID].life - life > 0) ? m_gameStates[userID].life - life : 0;
	int combo = m_gameStates[userID].combo;
	if (score > 0)
	{
		m_gameStates[userID].score += score * (1.1 + (combo / static_cast<double>(1000)));
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
	std::lock_guard<std::mutex> lockGuard(m_gameStateLock);

	Message msg;
	uint32_t sessionCount = NumberOfPeople();
	msg.PutData(reinterpret_cast<char*>(&sessionCount), sizeof(uint32_t));
	for (auto& gameState : m_gameStates)
	{
		uint64_t userID = gameState.first;
		uint64_t score = gameState.second.score;
		msg.PutData(reinterpret_cast<char*>(&userID), sizeof(userID));
		msg.PutData(reinterpret_cast<char*>(&score), sizeof(score));
	}
	msg.EncodeHeader(PacketType::ScoreBroadcast);

	milliseconds interval = duration_cast<milliseconds>(m_curTime - m_preTime);
	m_preTime = high_resolution_clock::now();
	std::cout << "[room: " << m_roomID << "] Score BroadCast [interval: " << interval.count() <<  "]\n";

	Deliver(msg);
}

