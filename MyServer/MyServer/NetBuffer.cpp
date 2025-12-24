#include "NetBuffer.h"
#include <vcruntime_string.h>

NetBuffer::NetBuffer()
{
	Init();
}

const char* NetBuffer::Data() const
{
	return m_data;
}

char* NetBuffer::Data()
{
	return m_data;
}

bool NetBuffer::Read(char* data, int len)
{
	if (len > UsingSize())
	{
		return false;
	}

	memcpy(data, m_data + m_beginIndex, len);
	m_beginIndex += len;

	if (m_beginIndex == m_endIndex)
	{
		Clear();
	}

	return true;
}

bool NetBuffer::Write(const char* data, int len)
{
	if (data == nullptr || len <= 0)
		return false;
	if (CheckOverflow(len))
		return false;

	if (GetDirectUsableSize() < len)
	{
		int usingSize = UsingSize();
		memmove(m_data, m_data + m_beginIndex, usingSize);
		m_beginIndex = 0;
		SetEndIndex(usingSize);
	}

	memcpy(m_data + m_endIndex, data, len);
	m_endIndex += len;

	return true;
}

int NetBuffer::UsingSize()
{
	return m_endIndex - m_beginIndex;
}

int NetBuffer::GetBufferSize()
{
	return sizeof(m_data);
}

int NetBuffer::GetDirectUsableSize()
{
	return GetBufferSize() - m_endIndex;
}

void NetBuffer::Clear()
{
	m_beginIndex = 0;
	m_endIndex = 0;
}

void NetBuffer::SetEndIndex(int endIndex)
{
	m_endIndex = endIndex;
}

void NetBuffer::Init()
{
	m_beginIndex = 0;
	m_endIndex = 0;
}

bool NetBuffer::CheckOverflow(int len)
{
	return GetBufferSize() - UsingSize() <= len;
}

