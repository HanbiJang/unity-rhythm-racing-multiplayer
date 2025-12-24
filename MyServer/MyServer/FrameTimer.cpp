#include <iostream>

#include "FrameTimer.h"

FrameTimer::FrameTimer(std::chrono::milliseconds limitTime)
{
	m_frameLimit = limitTime;
	m_remainingTime = std::chrono::milliseconds(0);
}

void FrameTimer::Begin()
{
	m_startTime = std::chrono::high_resolution_clock::now();
	m_expectedTime = m_startTime + m_frameLimit;
}

void FrameTimer::End()
{
	m_measureTime = std::chrono::high_resolution_clock::now();
}

bool FrameTimer::DoFrameSkip()
{
	m_measureTime = std::chrono::high_resolution_clock::now();
	std::chrono::milliseconds dur = std::chrono::duration_cast<std::chrono::milliseconds>(m_expectedTime - m_measureTime);
	//std::cout << "Frame Time " << m_remainingTime.count() << " millisec\n";
	if (m_measureTime < m_expectedTime)
	{
		if (m_remainingTime > std::chrono::milliseconds(0))
			m_remainingTime -= (std::chrono::duration_cast<std::chrono::milliseconds>(m_expectedTime - m_measureTime) < m_remainingTime) ? std::chrono::duration_cast<std::chrono::milliseconds>(m_expectedTime - m_measureTime) : m_remainingTime;
		else
			return true;
	}
	else
	{
		m_remainingTime += std::chrono::duration_cast<std::chrono::milliseconds>(m_measureTime - m_expectedTime);
	}
	m_expectedTime = m_measureTime + m_frameLimit;

	return false;
}
