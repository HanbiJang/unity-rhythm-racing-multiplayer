#pragma once
#include "Message.h"

const char* Message::Data() const
{
    return m_data;
}

char* Message::Data()
{
    return m_data;
}

std::size_t Message::MessageLength() const
{
    return sizeof(PacketHeader) + m_header.bodyLength;
}

bool Message::PutData(char* data, int len)
{
    if (len + m_header.bodyLength > MaxBodyLength)
        return false;
    memcpy(m_data + m_header.bodyLength, data, len);
    m_header.bodyLength += len;

    return true;
}

Message::PacketHeader& Message::Header()
{
    return m_header;
}

void Message::SetBodyLength(std::size_t newLength)
{
    m_header.bodyLength = newLength;
    if (m_header.bodyLength > MaxBodyLength)
        m_header.bodyLength = MaxBodyLength;
}

bool Message::DecodeHeader()
{
    memcpy(&m_header, m_data, sizeof(PacketHeader));
    if (m_header.bodyLength > MaxBodyLength)
    {
        m_header.bodyLength = 0;
        return false;
    }
    return true;
}

void Message::EncodeHeader(PacketType type)
{
    m_header.packetType = type;
}