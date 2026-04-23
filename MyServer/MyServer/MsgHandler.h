#pragma once
#include <mutex>
#include "Message.h"

void HandleInvalid(Message& msg);
void HandleReadyGame(Message& msg);
void HandleJudement(Message& msg);
void HandleRetryGame(Message& msg);
void HandleEndGame(Message& msg);
void HandleJoinGame(Message& msg);
void HandleLeave(Message& msg);
void HandleSpeedLevel(Message& msg);

class MsgHandler
{
public:
	MsgHandler();

	static MsgHandler* Instance();

	void HandlePacket(Message& msg);
	Message MakePacket(PacketType type, uint64_t roomID, uint64_t userID);
	void DoWork();

	void Push(Message& msg);

private:
	Message& Pop();

private:
	static MsgHandler* instance;
	MsgQueue m_globalQueue;

	std::mutex m_lock;
};
using MsgHandlerPtr = std::shared_ptr<MsgHandler>;
_declspec(selectany) MsgHandler* MsgHandler::instance = nullptr;
