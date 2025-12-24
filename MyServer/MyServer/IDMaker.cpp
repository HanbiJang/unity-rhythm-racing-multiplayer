#include <basetsd.h>

#include <iostream>
#include <mutex>

#include "IDMaker.h"

IDMaker::IDMaker()
{
	instance = this;
	std::random_device m_rd;
	m_generator = std::mt19937(m_rd());
	m_seedTime = high_resolution_clock::now();
}

IDMaker::~IDMaker()
{
}

// time 4bytes + random value 4bytes
uint64_t IDMaker::GetUniqueID()
{
	std::lock_guard<std::mutex> lockGuard(m_lock);
	uint64_t uniqueID;

	high_resolution_clock::time_point now = high_resolution_clock::now();
	milliseconds dur = duration_cast<milliseconds>(now - m_seedTime);
	uniqueID = dur.count();
	uniqueID <<= 32;

	int value = m_generator();
	uniqueID += value;

	return uniqueID;
}