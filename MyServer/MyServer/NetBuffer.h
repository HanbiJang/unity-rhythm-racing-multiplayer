#pragma once

class NetBuffer
{
public:
	enum Type
	{
		Default = 1024,
		Recv = 1024,
		Send = 2048,
	};

public:
	NetBuffer();
	const char* Data() const;
	char* Data();
	bool Read(char* data, int len);
	bool Write(const char* data, int len);
	
	int UsingSize();
	int GetBufferSize();
	int GetDirectUsableSize();
	void Clear();

	void SetEndIndex(int endIndex);

private:
	void Init();
	bool CheckOverflow(int len);

private:
	char m_data[1024];
	int m_beginIndex;
	int m_endIndex;
};

