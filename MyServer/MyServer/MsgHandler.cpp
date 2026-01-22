#pragma once
#include <functional>
#include <iostream>
#include <memory>
#include <mutex>
#include <chrono>

#include "Message.h"
#include "RoomManager.h"
#include "MsgHandler.h"

MsgHandler::MsgHandler()
{
	instance = this;
}

MsgHandler* MsgHandler::Instance()
{
	return instance;
}

void MsgHandler::HandlePacket(Message& msg)
{
	Message::PacketHeader& header = msg.Header();

	switch (header.packetType) {
	case PacketType::ReadyGame:
		HandleReadyGame(msg);
		break;
	case PacketType::Judgement:
		HandleJudement(msg);
		break;
	case PacketType::RetryGame:
		HandleRetryGame(msg);
		break;
	case PacketType::EndGame:
		HandleEndGame(msg);
		break;
	case PacketType::JoinGame:
		HandleJoinGame(msg);
		break;
	default:
		HandleInvalid(msg);
		break;
	}
}

Message MsgHandler::MakePacket(PacketType type, uint64_t roomID, uint64_t userID)
{
	Message msg;
	msg.PutData(reinterpret_cast<char*>(&userID), sizeof(uint64_t));
	msg.PutData(reinterpret_cast<char*>(&roomID), sizeof(uint64_t));
	msg.EncodeHeader(type);

	return msg;
}

void MsgHandler::DoWork()
{
	while (true)
	{
		if (m_globalQueue.empty())
		{
			Sleep(1);
			continue;
		}

		Message msg;
		{
			std::lock_guard<std::mutex> lockGuard(m_lock);
			msg = m_globalQueue.front();
			m_globalQueue.pop();
		}
		HandlePacket(msg);
	}
}

void MsgHandler::Push(Message& msg)
{
	std::lock_guard<std::mutex> lockGuard(m_lock);
	m_globalQueue.push(msg);
}

Message& MsgHandler::Pop()
{
	std::lock_guard<std::mutex> lockGuard(m_lock);

	Message msg = m_globalQueue.front();
	m_globalQueue.pop();
	return msg;
}

void HandleInvalid(Message& msg)
{
	std::cout << "Invalid Message [Type] " << msg.Header().packetType << " [Length] " << msg.Header().bodyLength << "\n";
}

void HandleReadyGame(Message& msg)
{
	CReadyGame* pkt = reinterpret_cast<CReadyGame*>(msg.Data());
	if (pkt == nullptr)
		return;

	RoomPtr room = RoomManager::Instance()->FindRoom(pkt->roomID);
	if (room == nullptr)
		return;

	room->SetReady(pkt->userID);
	if (msg.Header().bodyLength >= sizeof(CReadyGame))
	{
		room->SetMatchMode(pkt->matchMode);
		room->SetNickname(pkt->userID, pkt->nicknamePart1, pkt->nicknamePart2);
	}
	std::cout << "[User] " << pkt->userID << " is ready\n";
	if (room->ReadyCheck())
	{
		std::cout << "User All Ready\n";

		Message startGamePacket;
		const int kStartDelayMs = 2000;
		uint32_t userCount = room->NumberOfPeople();
		startGamePacket.PutData(reinterpret_cast<char*>(&userCount), sizeof(uint32_t));
		uint64_t* userList = room->GetUserList();
		startGamePacket.PutData(reinterpret_cast<char*>(userList), sizeof(uint64_t) * userCount);
		
		// 전체 음악 노트 개수 추가
		uint32_t totalNoteCount = static_cast<uint32_t>(room->GetTotalNoteCount());
		startGamePacket.PutData(reinterpret_cast<char*>(&totalNoteCount), sizeof(uint32_t));

		// 시작 시각(UTC ms) 추가 - 클라이언트 동기화용
		auto startSystem = std::chrono::system_clock::now() + std::chrono::milliseconds(kStartDelayMs);
		int64_t startUtcMs = std::chrono::duration_cast<std::chrono::milliseconds>(
			startSystem.time_since_epoch()).count();
		startGamePacket.PutData(reinterpret_cast<char*>(&startUtcMs), sizeof(int64_t));
		
		startGamePacket.EncodeHeader(PacketType::StartGame);

		std::cout << "Game Start!! Total Notes: " << totalNoteCount << " StartUtcMs: " << startUtcMs << "\n";
		room->Deliver(startGamePacket);
		room->StartAt(std::chrono::steady_clock::now() + std::chrono::milliseconds(kStartDelayMs));
	}
}

void HandleJudement(Message& msg)
{
	CJudgement* pkt = reinterpret_cast<CJudgement*>(msg.Data());
	if (pkt == nullptr)
		return;

	RoomPtr room = RoomManager::Instance()->FindRoom(pkt->roomID);
	if (room == nullptr)
		return;

	room->CalculateScore(pkt->userID, pkt->nodeType, pkt->judgmentType, pkt->timeDifference, pkt->score);
}

void HandleRetryGame(Message& msg)
{
	CRetryGame* pkt = reinterpret_cast<CRetryGame*>(msg.Data());
	if (pkt == nullptr)
		return;

	RoomPtr room = RoomManager::Instance()->FindRoom(pkt->roomID);
	if (room == nullptr)
		return;

	std::cout << "[User] " << pkt->userID << " requested retry\n";
	
	// 게임 상태 초기화
	room->ResetGame();
	
	// 모든 클라이언트에 RetryGame 패킷 브로드캐스트
	Message retryPacket;
	retryPacket.PutData(reinterpret_cast<char*>(&pkt->userID), sizeof(uint64_t));
	retryPacket.PutData(reinterpret_cast<char*>(&pkt->roomID), sizeof(uint64_t));
	retryPacket.EncodeHeader(PacketType::RetryGame);
	
	room->Deliver(retryPacket);
	std::cout << "[Room: " << pkt->roomID << "] Game reset for retry\n";
}

void HandleEndGame(Message& msg)
{
	std::cout << "End Game\n";
	CEndGame* pkt = reinterpret_cast<CEndGame*>(msg.Data());
	if (pkt == nullptr)
		return;

	RoomPtr room = RoomManager::Instance()->FindRoom(pkt->roomID);
	if (room == nullptr)
		return;
	room->EndGame();
}

void HandleJoinGame(Message& msg)
{
	CJoinGame* pkt = reinterpret_cast<CJoinGame*>(msg.Data());
	if (pkt == nullptr)
		return;

	RoomPtr room = RoomManager::Instance()->FindRoom(pkt->roomID);

	if (room == nullptr)
		return;

	room->SendTarget(msg, pkt->userID);
}

void HandleLeave(Message& msg)
{
	CReadyGame* pkt = reinterpret_cast<CReadyGame*>(msg.Data());
	if (pkt == nullptr)
		return;

	RoomPtr room = RoomManager::Instance()->FindRoom(pkt->roomID);
	if (room == nullptr)
		return;

	room->Leave(pkt->userID);
	room->Deliver(msg);
}