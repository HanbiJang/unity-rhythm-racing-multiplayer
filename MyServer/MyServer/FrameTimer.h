#pragma once
#include <chrono>

class FrameTimer
{
public:
	FrameTimer(std::chrono::milliseconds limitTime = std::chrono::milliseconds(100));
	
	void Begin();
	void End();
	bool DoFrameSkip();

private:
	std::chrono::high_resolution_clock::time_point m_startTime;		// 프레임 시작 시간
	std::chrono::high_resolution_clock::time_point m_expectedTime;	// 다음 프레임 시작 예상 시간
	std::chrono::high_resolution_clock::time_point m_measureTime;	// 실제 다음 프레임 시작 시간

	std::chrono::milliseconds m_frameLimit;							// 프레임 한계시간
	std::chrono::milliseconds m_remainingTime;						// 누적 측정 시간
};

