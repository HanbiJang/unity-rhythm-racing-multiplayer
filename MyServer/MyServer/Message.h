#pragma once
#include <basetsd.h>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdint>
#include <queue>

enum PacketType : int
{
	Invalid = 0,
	ReadyGame,
	Judgement,
	RetryGame,
	EndGame,
	JoinGame,
	StartGame,
	SpawnNode,
	ScoreBroadcast,
	Max,
};

class Message
{
public:
#pragma pack(push, 1)
	struct PacketHeader
	{
		PacketType packetType;
		int bodyLength;

		PacketHeader()
		{
			bodyLength = 0;
			packetType = PacketType::Invalid;
		}

		PacketHeader(int len, PacketType type)
		{
			bodyLength = len;
			packetType = type;
		}
	};
#pragma pack(pop)

	enum
	{
		MaxLength = 256,
		MaxBodyLength = 252,
	};

	const char* Data() const;
	char* Data();

	bool PutData(char* data, int len);

	size_t MessageLength() const;
	void SetBodyLength(std::size_t newLength);

	PacketHeader& Header();
	
	bool DecodeHeader();
	void EncodeHeader(PacketType type);
private:
	PacketHeader m_header;
	char m_data[MaxBodyLength];
};
using MsgQueue = std::queue<Message>;

#pragma pack(push, 1)
struct CReadyGame
{
	uint64_t userID;
	uint64_t roomID;
	uint32_t matchMode;
	uint64_t nicknamePart1;
	uint64_t nicknamePart2;
};
struct CJudgement
{
	uint64_t userID;
	uint64_t roomID;
	uint32_t nodeType;
};
struct CRetryGame
{
	uint64_t userID;
	uint64_t roomID;
};
struct CEndGame
{
	uint64_t userID;
	uint64_t roomID;
};
struct CJoinGame
{
	uint64_t userID;
	uint64_t roomID;
};
struct CStartGame
{
	uint32_t userCount;
	uint64_t* userList;
};
struct CSpawnNode
{
	uint32_t nodeType;
	uint32_t nodePos;
};
struct CScoreBroadcast
{
	uint64_t userID;
	uint64_t roomID;
};
struct CLeave
{
	uint64_t userID;
	uint64_t roomID;
};
#pragma pack(pop)