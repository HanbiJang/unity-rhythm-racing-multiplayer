#pragma once
#include <chrono>
#include <random>

#include "Singleton.h"

using namespace std::chrono;

// Make Unique ID from time and random value
class IDMaker : public Singleton<IDMaker>
{
public:
	IDMaker();
	virtual ~IDMaker();

	uint64_t GetUniqueID();

private:
	std::mt19937 m_generator;
	high_resolution_clock::time_point m_seedTime;

	std::mutex m_lock;

	static IDMaker* instance;
};
_declspec(selectany) IDMaker* IDMaker::instance = nullptr;